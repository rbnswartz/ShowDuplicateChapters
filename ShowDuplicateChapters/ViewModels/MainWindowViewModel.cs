using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using BTTWriterLib;
using DynamicData;
using ReactiveUI;
using USFMToolsSharp;
using USFMToolsSharp.Models.Markers;

namespace ShowDuplicateChapters.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        // Add an open file command
        public ReactiveCommand<Unit, Task> OpenFileCommand { get; }
        public string FilePath { get; set; }
        
        public ObservableCollection<string> Results { get; set; } = new ObservableCollection<string>();


        public MainWindowViewModel()
        {
            // Initialize the open file command
            OpenFileCommand = ReactiveCommand.Create(async () =>
            {
                // Show the open file dialog
                var dialog = new OpenFolderDialog();
                if (Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
                {
                    var result = await dialog.ShowAsync(lifetime.MainWindow);
                    // If the user selected a file, set the FilePath property
                    if (result != null && result.Length > 0)
                    {
                        FilePath = result;
                        Results.Clear();
                        Results.AddRange(GetDuplicateChapters(result));
                    }
                }
            });
        }

        public List<string> GetDuplicateChapters(string sourceDir)
        {
            var output = new List<string>();
            // if a manifest.json file exists load using bttwriterloader
            if (File.Exists(Path.Combine(sourceDir, "manifest.json")))
            {
                var document =  BTTWriterLoader.CreateUSFMDocumentFromContainer(new FileSystemResourceContainer(sourceDir), false);
                output.AddRange(document.GetChildMarkers<CMarker>().Select(i => i.Number).GroupBy(i => i)
                    .Where(i => i.Count() > 1)
                        .Select(i => $"{Path.Combine(sourceDir,i.Key.ToString(),"01.txt")} Chapter: {i.Key}").ToList());
            }
            else
            {
                var parser = new USFMParser();
                // Loop through all usfm files
                foreach (var file in Directory.EnumerateFiles(sourceDir,"*.usfm", SearchOption.AllDirectories))
                {
                    var document = parser.ParseFromString(File.ReadAllText(file));
                    output.AddRange(document.GetChildMarkers<CMarker>().Select(i => i.Number).GroupBy(i => i)
                        .Where(i => i.Count() > 1)
                        .Select(i => $"{file} Chapter: {i.Key}").ToList());
                }
                
            }
            return output;
        }

    }
}