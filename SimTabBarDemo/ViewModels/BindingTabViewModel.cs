using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace SimTabBarDemo.ViewModels;

public partial class BindingTabViewModel : ObservableObject
{
    [ObservableProperty]
    private DocumentItem? _selectedDocument;

    public ObservableCollection<DocumentItem> Documents { get; }

    public BindingTabViewModel()
    {
        Documents = new ObservableCollection<DocumentItem>
        {
            new("文档1.txt", "文档 1 的内容"),
            new("文档2.txt", "文档 2 的内容"),
            new("文档3.txt", "文档 3 的内容"),
        };
        SelectedDocument = Documents.FirstOrDefault();
    }

    [RelayCommand]
    private void AddDocument()
    {
        var doc = new DocumentItem($"文档{Documents.Count + 1}.txt", $"新文档内容");
        Documents.Add(doc);
        SelectedDocument = doc;
    }
}

public class DocumentItem
{
    public string Title { get; }
    public string Description { get; }

    public DocumentItem(string title, string description)
    {
        Title = title;
        Description = description;
    }

    public override string ToString() => Title;
}
