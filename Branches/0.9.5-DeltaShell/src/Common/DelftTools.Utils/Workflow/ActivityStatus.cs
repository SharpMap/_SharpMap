namespace DelftTools.Utils.Workflow
{
    /// <summary>
    /// Defines possible states of the activity.
    /// 
    /// TODO: migrate to WWF-based implementaiton in .NET 3.5
    /// </summary>
    public enum ActivityStatus
    {
        /// <summary>
        /// Activity has been just created and not used yet.
        /// </summary>
        None,

        /// <summary>
        /// Activity is being initialized.
        /// </summary>
        Initializing,

        /// <summary>
        /// Activity has been initialized and ready for execution.
        /// </summary>
        Initialized,

        /// <summary>
        /// Activity is currently running.
        /// </summary>
        Running,

        /// <summary>
        /// Activity has run and finished successfully.
        /// </summary>
        Finished,

        /// <summary>
        /// Activity has run but failed to complete.
        /// </summary>
        Failed,

        /// <summary>
        /// Activite execution is being cancelled.
        /// </summary>
        Cancelling,

        /// <summary>
        /// Activity execution has been cancelled.
        /// </summary>
        Cancelled,

        /// <summary>
        /// Activity can't progress yet.
        /// </summary>
        WaitingForData
    }

    /* 
    WWF defines the following statuses for Activity:
    
    public enum ActivityStatus
    {
        Initialized, // Represents the status when an activity is being initialized. 
        Running, // Represents the status when an activity is executing. 
        Canceling, // Represents the status when an activity is in the process of being canceled. 
        Closed, // Represents the status when an activity is closed. 
        Compensating, // Represents the status when an activity is compensating. 
        Faulting // Represents the status when an activity is faulting. 
    } 
     */
}