namespace DelftTools.TestUtils
{
    /// <summary>
    /// [Uncategorized] .............. unit tests (less than 500ms), or marked with some custom categories<br/>
    ///<br/>
    /// The following categories started with prefix "Build." are used for integration tests which are handled <br/>
    /// in a special way on a build server:<br/>
    /// <br/>
    /// Build.Integration ............ general integration tests (many classes, almost no mocks, mix of windows.forms, data access)<br/>
    ///     Build.Performance. ....... integration tests which assert execution time using <see cref="TestHelper.AssertIsFasterThan(float,System.Action)"/><br/>
    ///     Build.WindowsForms ....... integration tests which pop-up forms<br/>
    ///     Build.DataAccess ......... integration tests which focus on reading/writing<br/>
    /// <br/>
    /// These categories must be always used as mutually exclusive: NEVER USE 2 OF THESE CATEGORIES AT THE SAME TIME!<br/>
    ///<br/>
    /// Exceptions from the rule above (use of 2 categories at the same time):<br/>
    /// <br/>
    /// Build.WorkInProgress ..... tets which are currently in development<br/>
    /// Build.Slow ............... this can be used together with other categories<br/>
    /// 
    /// </summary>
    public class TestCategory
    {
        /// <summary>
        /// Creates multiple components and tests how they work together.
        /// 
        /// Speed requirements: faster than 5000 ms
        /// </summary>
        public const string Integration = "Build.Integration";

        /// <summary>
        /// Tests access to the file system.
        /// 
        /// Speed requirements: faster than 2000 ms
        /// </summary>
        public const string DataAccess = "Build.DataAccess";

        /// <summary>
        /// Shows forms or dialogs during run.
        /// 
        /// Speed requirements: faster than 4000 ms
        /// </summary>
        public const string WindowsForms = "Build.WindowsForms";

        /// <summary>
        /// Checks how fast specific code runs. 
        /// Speed requirements: faster than 20000 ms
        /// <seealso cref="TestHelper.AssertIsFasterThan(float,System.Action)"/>
        /// </summary>
        public const string Performance = "Build.Performance";

        /// <summary>
        /// Test is incomplete and unfinished.
        /// </summary>
        public const string WorkInProgress = "Build.WorkInProgress";

        /// <summary>
        /// Takes long time to run
        /// Speed requirements: faster than 500 ms (for unit tests)
        /// </summary>
        public const string Slow = "Build.Slow";

        /// <summary>
        /// Takes very long time to run, usually runs nightly.
        /// Speed requirements: > 20 sec
        /// </summary>
        public const string VerySlow = "Build.VerySlow";

        /// <summary>
        /// Tests that test undo/redo functionality
        /// </summary>
        public const string UndoRedo = "UndoRedo";

        /// <summary>
        /// Requires license in order to run.
        /// </summary>
        public const string RequiresLicense = "RequiresLicense";

        /// <summary>
        /// Reproduces JIRA issue.
        /// </summary>
        public const string Jira = "JIRA";

        /// <summary>
        /// Check how new version works with old files or components.
        /// </summary>
        public const string BackwardCompatibility = "BackwardCompatibility";

        /// <summary>
        /// Bad test - improve or remove!
        /// </summary>
        public const string BadQuality = "BadQuality";

        public const string MemoryLeak = "MemoryLeak";
    }
}