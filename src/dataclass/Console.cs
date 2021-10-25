namespace BMG
{
    public abstract class ConsoleOptionsBase
    {
        public abstract BMGEvent EventFilter { get; }
    }


    public abstract class TitleOptionsBase
    {
        public abstract class AppInfoBase
        {
            public abstract bool ShowVersion { get; }
        }

        public abstract class ProgressBarBase
        {
            public abstract char Full { get; }
            public abstract char Empty { get; }
        }

        public abstract class JobBase : ProgressBarBase
        {
            public abstract string Layout { get; }
        }

        public abstract class StatusBase : ProgressBarBase
        {
            public abstract string Layout { get; }
        }

        public abstract class StatusDetailsBase
        {
            public abstract bool ShowBiome { get; }
            public abstract bool ShowTile { get; }
        }

        public abstract AppInfoBase AppInfo { get; }
        public abstract JobBase Job { get; }
        public abstract StatusBase Status { get; }
        public abstract StatusDetailsBase StatusDetails { get; }
        public abstract string Layout { get; }
        public abstract bool UpdateEnabled { get; }
    }
}
