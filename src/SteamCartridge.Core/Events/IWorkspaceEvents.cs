using SteamCartridge.Core.Workspace;

namespace SteamCartridge.Core.Events
{
    /// <summary>
    /// Notifications raised when the workspace cache changes.
    /// View models subscribe to these events and project them onto the UI.
    /// Commands update the cache; they do not update the view themselves.
    /// </summary>
    public interface IWorkspaceEvents
    {
        /// <summary>
        /// Raised when categories are loaded from disk.
        /// </summary>
        event EventHandler<List<Category>>? CategoriesLoaded;

        /// <summary>
        /// Raised when a category is added to the cache.
        /// </summary>
        event EventHandler<Category>? CategoryAdded;

        /// <summary>
        /// Raised when a category is removed from the cache.
        /// </summary>
        event EventHandler<Category>? CategoryRemoved;

        /// <summary>
        /// Raised when every category is cleared from the cache.
        /// </summary>
        event EventHandler? CategoriesCleared;

        /// <summary>
        /// Raised when a game is added to or removed from a category in the cache.
        /// </summary>
        event EventHandler<CategoryGameChangeEventArgs>? CategoryGameChanged;

        /// <summary>
        /// Raised when a game enters the cache for the first time.
        /// </summary>
        event EventHandler<GameData>? GameCached;

        /// <summary>
        /// Raised when a property of a cached game changes.
        /// </summary>
        event EventHandler<GameCacheChangeEventArgs>? GameUpdated;
    }

    /// <summary>
    /// Describes a property change on a game that is already in the cache.
    /// </summary>
    public sealed class GameCacheChangeEventArgs : EventArgs
    {
        public GameCacheChangeEventArgs(GameData game, string? propertyName)
        {
            Game = game;
            PropertyName = propertyName;
        }

        public GameData Game { get; }
        public string? PropertyName { get; }
    }

    /// <summary>
    /// Describes a game moving into or out of a category.
    /// </summary>
    public sealed class CategoryGameChangeEventArgs : EventArgs
    {
        public CategoryGameChangeEventArgs(Category category, GameData game, bool added)
        {
            Category = category;
            Game = game;
            Added = added;
        }

        public Category Category { get; }
        public GameData Game { get; }
        public bool Added { get; }
    }
}
