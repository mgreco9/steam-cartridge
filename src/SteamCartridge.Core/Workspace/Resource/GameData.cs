using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SteamCartridge.Core.Workspace;

public class GameData : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public required int GameID { get; set; }

    private string _name = "";
    public string Name
    {
        get => _name;
        set => SetField(ref _name, value);
    }

    private DateTime _releaseDate;
    public DateTime ReleaseDate
    {
        get => _releaseDate;
        set => SetField(ref _releaseDate, value);
    }

    private CartridgeDesignState _designState = CartridgeDesignState.NOT_DESIGNED;
    public CartridgeDesignState DesignState
    {
        get => _designState;
        set => SetFieldAlwaysNotify(ref _designState, value);
    }

    private CartridgePhysicalState _physicalState = CartridgePhysicalState.NOT_ASSIGNED;
    public CartridgePhysicalState PhysicalState
    {
        get => _physicalState;
        set => SetField(ref _physicalState, value);
    }

    private string _genre = "Undefined";
    public string Genre
    {
        get => _genre;
        set => SetField(ref _genre, value);
    }

    private string _franchise = "Undefined";
    public string Franchise
    {
        get => _franchise;
        set => SetField(ref _franchise, value);
    }

    private string _classification = ".";
    public string Classification
    {
        get => _classification;
        set => SetField(ref _classification, value);
    }

    private string _numerotation = "0";
    public string Numerotation
    {
        get => _numerotation;
        set => SetField(ref _numerotation, value);
    }

    private List<string> _tagList = [];
    public List<string> TagList
    {
        get => _tagList;
        set => SetField(ref _tagList, value);
    }

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return;

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void SetFieldAlwaysNotify<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
