using System;
using System.IO;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using AvaloniaApplication1.AudioEngine;
using AvaloniaApplication1.ViewModels;
using NAudio.Wave;

namespace AvaloniaApplication1.Views;

public partial class MainWindow : Window
{
    private static readonly FilePickerFileType JsonFileType = new("JSON preset")
    {
        Patterns = new[] { "*.json" },
        MimeTypes = new[] { "application/json" }
    };

    private static readonly FilePickerFileType WavFileType = new("WAV audio")
    {
        Patterns = new[] { "*.wav" },
        MimeTypes = new[] { "audio/wav" }
    };

    public MainWindow()
    {
        InitializeComponent();

        AddHandler(KeyDownEvent, OnWindowKeyDown, RoutingStrategies.Tunnel);
        AddHandler(KeyUpEvent, OnWindowKeyUp, RoutingStrategies.Tunnel);

        Deactivated += (_, _) =>
        {
            if (DataContext is MainWindowViewModel viewModel)
            {
                viewModel.ReleaseComputerKeys();
            }
        };

        Closed += (_, _) =>
        {
            if (DataContext is IDisposable disposable)
            {
                disposable.Dispose();
            }
        };
    }

    private void OnWindowKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel || IsTextInputFocused())
        {
            return;
        }

        if (e.KeyModifiers.HasFlag(KeyModifiers.Control) ||
            e.KeyModifiers.HasFlag(KeyModifiers.Alt) ||
            e.KeyModifiers.HasFlag(KeyModifiers.Meta))
        {
            return;
        }

        if (viewModel.HandleKeyDown(e.Key))
        {
            e.Handled = true;
        }
    }

    private void OnWindowKeyUp(object? sender, KeyEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        if (viewModel.HandleKeyUp(e.Key))
        {
            e.Handled = true;
        }
    }

    private bool IsTextInputFocused()
    {
        object? focused = FocusManager?.GetFocusedElement();
        return focused is TextBox || focused is ComboBox { IsDropDownOpen: true };
    }

    private async void OnSavePresetClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        try
        {
            IStorageFile? file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Save preset",
                SuggestedFileName = "Obsidian Preset.json",
                DefaultExtension = "json",
                ShowOverwritePrompt = true,
                FileTypeChoices = new[] { JsonFileType }
            });

            if (file == null)
            {
                return;
            }

            await using Stream stream = await file.OpenWriteAsync();
            await using var writer = new StreamWriter(stream);
            await writer.WriteAsync(viewModel.ExportPresetJson());
            viewModel.StatusText = $"Preset saved · {file.Name}";
        }
        catch
        {
            viewModel.StatusText = "Could not save preset";
        }
    }

    private async void OnLoadPresetClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        try
        {
            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Load preset",
                AllowMultiple = false,
                FileTypeFilter = new[] { JsonFileType }
            });

            if (files.Count == 0)
            {
                return;
            }

            await using Stream stream = await files[0].OpenReadAsync();
            using var reader = new StreamReader(stream);
            string json = await reader.ReadToEndAsync();
            viewModel.ImportPresetJson(json);
        }
        catch
        {
            viewModel.StatusText = "Could not load preset";
        }
    }

    private async void OnRecordClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        if (!viewModel.IsRecording)
        {
            viewModel.StartRecording();
            return;
        }

        float[] samples = viewModel.StopRecording();

        if (samples.Length == 0)
        {
            viewModel.StatusText = "Nothing recorded";
            return;
        }

        try
        {
            IStorageFile? file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Export WAV",
                SuggestedFileName = "Obsidian Take.wav",
                DefaultExtension = "wav",
                ShowOverwritePrompt = true,
                FileTypeChoices = new[] { WavFileType }
            });

            if (file == null)
            {
                viewModel.StatusText = "Take discarded";
                return;
            }

            using Stream stream = await file.OpenWriteAsync();
            byte[] bytes = new byte[samples.Length * 2];

            for (int i = 0; i < samples.Length; i++)
            {
                int value = (int)Math.Clamp(samples[i] * 32767f, -32768f, 32767f);
                bytes[i * 2] = (byte)(value & 0xFF);
                bytes[i * 2 + 1] = (byte)((value >> 8) & 0xFF);
            }

            using (var writer = new WaveFileWriter(stream, new WaveFormat(SynthEngine.SampleRate, 16, 1)))
            {
                writer.Write(bytes, 0, bytes.Length);
            }

            viewModel.StatusText = $"WAV saved · {file.Name}";
        }
        catch
        {
            viewModel.StatusText = "Could not export WAV";
        }
    }
}
