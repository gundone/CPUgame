using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Xna.Framework;
using CPUgame.Components;

namespace CPUgame.Core;

public static class CircuitSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static void SaveCircuit(Circuit circuit, string filePath)
    {
        var data = SerializeCircuit(circuit);
        var json = JsonSerializer.Serialize(data, Options);
        File.WriteAllText(filePath, json);
    }

    public static Circuit LoadCircuit(string filePath, Dictionary<string, CircuitData>? customComponents = null)
    {
        var json = File.ReadAllText(filePath);
        var data = JsonSerializer.Deserialize<CircuitData>(json, Options);
        if (data == null) throw new InvalidOperationException("Failed to deserialize circuit");
        return DeserializeCircuit(data, customComponents);
    }

    public static void SaveCustomComponent(Circuit circuit, string name, string filePath)
    {
        var data = SerializeCircuit(circuit);
        data.Name = name;
        data.IsCustomComponent = true;
        var json = JsonSerializer.Serialize(data, Options);
        File.WriteAllText(filePath, json);
    }

    public static CircuitData LoadCustomComponentData(string filePath)
    {
        var json = File.ReadAllText(filePath);
        var data = JsonSerializer.Deserialize<CircuitData>(json, Options);
        if (data == null) throw new InvalidOperationException("Failed to deserialize component");
        return data;
    }

    public static CircuitData SerializeCircuit(Circuit circuit)
    {
        var data = new CircuitData
        {
            Name = circuit.Name,
            Components = new List<ComponentData>(),
            Connections = new List<ConnectionData>()
        };

        var componentIds = new Dictionary<Component, int>();
        int id = 0;

        foreach (var component in circuit.Components)
        {
            componentIds[component] = id;
            var compData = new ComponentData
            {
                Id = id,
                Type = GetComponentType(component),
                X = component.X,
                Y = component.Y,
                Title = component.Title
            };

            if (component is InputSwitch sw)
                compData.State = sw.IsOn;
            else if (component is Clock clk)
                compData.Frequency = clk.Frequency;
            else if (component is CustomComponent custom)
                compData.CustomName = custom.ComponentName;
            else if (component is BusInput busIn)
            {
                compData.BitCount = busIn.BitCount;
                compData.Value = busIn.Value;
            }
            else if (component is BusOutput busOut)
            {
                compData.BitCount = busOut.BitCount;
            }

            data.Components.Add(compData);
            id++;
        }

        // Save connections
        foreach (var component in circuit.Components)
        {
            foreach (var input in component.Inputs)
            {
                if (input.ConnectedTo != null)
                {
                    var fromComponent = input.ConnectedTo.Owner;
                    var fromPinIndex = fromComponent.Outputs.IndexOf(input.ConnectedTo);
                    var toComponent = component;
                    var toPinIndex = toComponent.Inputs.IndexOf(input);

                    var connData = new ConnectionData
                    {
                        FromComponentId = componentIds[fromComponent],
                        FromPinIndex = fromPinIndex,
                        ToComponentId = componentIds[toComponent],
                        ToPinIndex = toPinIndex
                    };

                    // Save manual wire path if present
                    if (input.ManualWirePath != null && input.ManualWirePath.Count > 0)
                    {
                        connData.ManualWirePath = new List<int[]>();
                        foreach (var point in input.ManualWirePath)
                        {
                            connData.ManualWirePath.Add(new[] { point.X, point.Y });
                        }
                    }

                    data.Connections.Add(connData);
                }
            }
        }

        return data;
    }

    public static Circuit DeserializeCircuit(CircuitData data, Dictionary<string, CircuitData>? customComponents = null)
    {
        var circuit = new Circuit { Name = data.Name };
        var componentMap = new Dictionary<int, Component>();

        foreach (var compData in data.Components)
        {
            Component? component = compData.Type switch
            {
                "NAND" => new NandGate(compData.X, compData.Y),
                "Switch" => new InputSwitch(compData.X, compData.Y, compData.State ?? false),
                "LED" => new OutputLed(compData.X, compData.Y),
                "Clock" => new Clock(compData.X, compData.Y) { Frequency = compData.Frequency ?? 1.0 },
                "BusInput" => new BusInput(compData.X, compData.Y, compData.BitCount ?? 4) { Value = compData.Value ?? 0 },
                "BusOutput" => new BusOutput(compData.X, compData.Y, compData.BitCount ?? 4),
                "Custom" when compData.CustomName != null && customComponents != null
                    && customComponents.TryGetValue(compData.CustomName, out var customData)
                    => new CustomComponent(compData.X, compData.Y, compData.CustomName,
                        DeserializeCircuit(customData, customComponents)),
                _ => null
            };

            if (component != null)
            {
                // Set title before adding to avoid auto-generation
                if (!string.IsNullOrEmpty(compData.Title))
                {
                    component.Title = compData.Title;
                }

                circuit.AddComponent(component);
                componentMap[compData.Id] = component;
            }
        }

        // Restore connections
        foreach (var conn in data.Connections)
        {
            if (componentMap.TryGetValue(conn.FromComponentId, out var fromComp) &&
                componentMap.TryGetValue(conn.ToComponentId, out var toComp))
            {
                if (conn.FromPinIndex < fromComp.Outputs.Count &&
                    conn.ToPinIndex < toComp.Inputs.Count)
                {
                    var fromPin = fromComp.Outputs[conn.FromPinIndex];
                    var toPin = toComp.Inputs[conn.ToPinIndex];
                    fromPin.Connect(toPin);

                    // Restore manual wire path if present
                    if (conn.ManualWirePath != null && conn.ManualWirePath.Count > 0)
                    {
                        toPin.ManualWirePath = new List<Point>();
                        foreach (var pointArray in conn.ManualWirePath)
                        {
                            if (pointArray.Length >= 2)
                            {
                                toPin.ManualWirePath.Add(new Point(pointArray[0], pointArray[1]));
                            }
                        }
                    }
                }
            }
        }

        // Update counters to avoid duplicate titles for new components
        circuit.UpdateCountersFromComponents();

        return circuit;
    }

    private static string GetComponentType(Component component)
    {
        return component switch
        {
            NandGate => "NAND",
            InputSwitch => "Switch",
            OutputLed => "LED",
            Clock => "Clock",
            BusInput => "BusInput",
            BusOutput => "BusOutput",
            CustomComponent => "Custom",
            _ => "Unknown"
        };
    }
}

public class CircuitData
{
    public string Name { get; set; } = "";
    public bool IsCustomComponent { get; set; }
    public List<ComponentData> Components { get; set; } = new();
    public List<ConnectionData> Connections { get; set; } = new();
}

public class ComponentData
{
    public int Id { get; set; }
    public string Type { get; set; } = "";
    public int X { get; set; }
    public int Y { get; set; }
    public string? Title { get; set; }
    public bool? State { get; set; }
    public double? Frequency { get; set; }
    public string? CustomName { get; set; }
    public int? BitCount { get; set; }
    public int? Value { get; set; }
}

public class ConnectionData
{
    public int FromComponentId { get; set; }
    public int FromPinIndex { get; set; }
    public int ToComponentId { get; set; }
    public int ToPinIndex { get; set; }
    public List<int[]>? ManualWirePath { get; set; }
}
