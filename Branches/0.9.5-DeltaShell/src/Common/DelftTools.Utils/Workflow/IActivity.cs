using System;

namespace DelftTools.Utils.Workflow
{
    /// <summary>
    /// Defines basic activity which can be executed as part of the workflow.
    /// </summary>
    /// </summary>
    public interface IActivity:INameable
    {
        /// <summary>
        /// Returns current status of the activity (executing, cancelling, etc.)
        /// </summary>uit
        ActivityStatus Status { get; }

        /// <summary>
        /// Initializes activity, it initialization step is successful - activity status will change to Initialized.
        /// </summary>
        void Initialize();

        /// <summary>
        /// Executes activity. Depending on status of the activity execution may need to be repeated.
        /// </summary>
        /// <returns></returns>
        void Execute();

        /// <summary>
        /// Cancel activity execution (if it is in Running state).
        /// </summary>
        void Cancel();
    }
}