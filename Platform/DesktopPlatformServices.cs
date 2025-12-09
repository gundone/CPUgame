using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using CPUgame.Core.Services;

namespace CPUgame.Platform;

/// <summary>
/// Desktop (Windows/Linux/Mac) implementation of platform services
/// </summary>
public class DesktopPlatformServices : IPlatformServices
{
    private readonly string _baseDirectory;

    public DesktopPlatformServices()
    {
        _baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
    }

    public string GetComponentsFolder()
    {
        return Path.Combine(_baseDirectory, "Components");
    }

    public string GetSavesFolder()
    {
        return _baseDirectory;
    }

    public string GetDefaultCircuitPath()
    {
        return Path.Combine(_baseDirectory, "circuit.json");
    }

    public string CombinePath(params string[] paths)
    {
        return Path.Combine(paths);
    }

    public bool DirectoryExists(string path)
    {
        return Directory.Exists(path);
    }

    public void EnsureDirectoryExists(string path)
    {
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
    }

    public bool FileExists(string path)
    {
        return File.Exists(path);
    }

    public void DeleteFile(string path)
    {
        File.Delete(path);
    }

    public IEnumerable<string> GetFiles(string directory, string pattern)
    {
        if (!Directory.Exists(directory))
            return Array.Empty<string>();

        return Directory.GetFiles(directory, pattern);
    }

    public string ReadAllText(string path)
    {
        return File.ReadAllText(path);
    }

    public void WriteAllText(string path, string content)
    {
        File.WriteAllText(path, content);
    }

    public FileDialogResult ShowSaveFileDialog(string title, string defaultFileName, string filter, string? initialDirectory = null)
    {
        FileDialogResult dialogResult = new();

        RunOnStaThread(() =>
        {
            using var dialog = new SaveFileDialog
            {
                Title = title,
                FileName = defaultFileName,
                Filter = filter,
                DefaultExt = ".json",
                AddExtension = true,
                InitialDirectory = initialDirectory ?? GetSavesFolder()
            };

            var result = dialog.ShowDialog();
            dialogResult.Success = result == DialogResult.OK;
            dialogResult.FilePath = result == DialogResult.OK ? dialog.FileName : null;
        });

        return dialogResult;
    }

    public FileDialogResult ShowOpenFileDialog(string title, string filter, string? initialDirectory = null)
    {
        FileDialogResult dialogResult = new();

        RunOnStaThread(() =>
        {
            using var dialog = new OpenFileDialog
            {
                Title = title,
                Filter = filter,
                InitialDirectory = initialDirectory ?? GetSavesFolder()
            };

            var result = dialog.ShowDialog();
            dialogResult.Success = result == DialogResult.OK;
            dialogResult.FilePath = result == DialogResult.OK ? dialog.FileName : null;
        });

        return dialogResult;
    }

    private static void RunOnStaThread(Action action)
    {
        if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
        {
            action();
            return;
        }

        Exception? exception = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                exception = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (exception != null)
        {
            throw exception;
        }
    }
}
