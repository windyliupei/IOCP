using System;
using System.Diagnostics;
using System.Text;

namespace IOCPServer
{
    
    //枚举日志优先级条目
    public enum LogPrio
    {
        /// <summary>
        /// 追踪程序流的日志条目
        /// </summary>
        Trace,

        /// <summary>
        /// 帮助debug应用的日志条目
        /// </summary>
        Debug,

        /// <summary>
        /// 追踪状态改变信息
        /// </summary>
        Info,

        /// <summary>
        /// 警告信息
        /// </summary>
        Warning,

        /// <summary>
        /// 错误
        /// </summary>
        Error,

        /// <summary>
        /// 严重错误
        /// </summary>
        Fatal
    }

    /// <summary>
    /// 写日志文件接口ILogWriter
    /// </summary>

    public interface ILogWriter
    {
        /// <summary>
        /// 向日志文件写一条信息
        /// </summary>
       
        void Write(object source, LogPrio priority, string message);
    }

    /// <summary>
    /// 接口实现
    /// </summary>
    public sealed class ConsoleLogWriter : ILogWriter
    {
        public static readonly ConsoleLogWriter Instance = new ConsoleLogWriter();

        /// <summary>
        /// 写日志消息，实现接口
        /// </summary>

        public void Write(object source, LogPrio prio, string message)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(DateTime.Now.ToString());
            sb.Append(" ");
            sb.Append(prio.ToString().PadRight(5));
            sb.Append(" (");
#if DEBUG
            StackTrace trace = new StackTrace();
            StackFrame[] frames = trace.GetFrames();
            int endFrame = frames.Length > 4 ? 4 : frames.Length;
            int startFrame = frames.Length > 0 ? 1 : 0;
            for (int i = startFrame; i < endFrame - 1; ++i)
            {
                sb.Append(frames[i].GetMethod().Name);
                sb.Append(".");
            }
            sb.Append(frames[endFrame - 1].GetMethod().Name);
            sb.Append(") ");
#else
            sb.Append(System.Reflection.MethodBase.GetCurrentMethod().Name);
            sb.Append(" | ");
#endif
            sb.Append(message);

            Console.ForegroundColor = GetColor(prio);
            Console.WriteLine(sb.ToString());
            Console.ForegroundColor = ConsoleColor.Gray;

            //将日志写入目标文件
            string logPath = @"Log\log.txt"; //文件相对路径
            if (System.IO.File.Exists(logPath))
            {
                System.IO.StreamWriter sw = new System.IO.StreamWriter(logPath, true, Encoding.UTF8);
                sw.WriteLine(sb);
                sw.Close();
            }

        }

        /// <summary>
        /// 将不同优先级的日志项异色显示
        /// </summary>
        public static ConsoleColor GetColor(LogPrio prio)
        {
            switch (prio)
            {
                case LogPrio.Trace:
                    return ConsoleColor.DarkGray;
                case LogPrio.Debug:
                    return ConsoleColor.Gray;
                case LogPrio.Info:
                    return ConsoleColor.White;
                case LogPrio.Warning:
                    return ConsoleColor.DarkMagenta;
                case LogPrio.Error:
                    return ConsoleColor.Magenta;
                case LogPrio.Fatal:
                    return ConsoleColor.Red;
            }

            return ConsoleColor.Yellow;
        }
    }

    /// <summary>
    /// 默认的日志Writer
    /// </summary>

    public sealed class NullLogWriter : ILogWriter
    {

        public static readonly NullLogWriter Instance = new NullLogWriter();

        /// <summary>
        /// 显示为空
        /// </summary>
        public void Write(object source, LogPrio prio, string message)
        {}
    }
}