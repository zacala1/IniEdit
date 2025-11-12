namespace IniEdit.GUI.Commands
{
    /// <summary>
    /// Interface for all undoable commands
    /// </summary>
    public interface ICommand
    {
        /// <summary>
        /// Execute the command
        /// </summary>
        void Execute();

        /// <summary>
        /// Undo the command
        /// </summary>
        void Undo();

        /// <summary>
        /// Description of the command for debugging/UI
        /// </summary>
        string Description { get; }
    }
}
