using System;
using System.Net;
using System.IO;
using System.Collections.Generic;
using System.Text; //for testing

namespace SocketAsyncServer
{
    class Program
    {
        //If you make this true, then connect/disconnect info will print to log.
        public static readonly bool watchConnectAndDisconnect = true;

        //If you make this true, then data will print to log.        
        public static readonly bool watchData = true;        

        //If you make this true, then the IncomingDataPreparer will not write to
        // a List<T>, and you will not see the printout of the data at the end
        //of the log.
        public static readonly bool runLongTest = false;

        //If you make this true, then info about threads will print to log.
        public static readonly bool watchThreads = false;

        //If you make this true, then the above "watch-" variables will print to
        //both Console and log, instead of just to log. I suggest only using this if
        //you are having a problem with an application that is crashing.
        public static readonly bool consoleWatch = false;
        
        //This variable determines the number of 
        //SocketAsyncEventArg objects put in the pool of objects for receive/send.
        //The value of this variable also affects the Semaphore.
        //This app uses a Semaphore to ensure that the max # of connections
        //value does not get exceeded.
        //Max # of connections to a socket can be limited by the Windows Operating System
        //also.
        public const Int32 maxNumberOfConnections = 3000;

        //If this port # will not work for you, it's okay to change it.
        public const Int32 port = 2020;

        //You would want a buffer size larger than 25 probably, unless you know the
        //data will almost always be less than 25. It is just 25 in our test app.
        public const Int32 testBufferSize = 25;
                        
        //This is the maximum number of asynchronous accept operations that can be 
        //posted simultaneously. This determines the size of the pool of 
        //SocketAsyncEventArgs objects that do accept operations. Note that this
        //is NOT the same as the maximum # of connections.
        public const Int32 maxSimultaneousAcceptOps = 10;

        //The size of the queue of incoming connections for the listen socket.
        public const Int32 backlog = 100;

        //For the BufferManager
        public const Int32 opsToPreAlloc = 2;    // 1 for receive, 1 for send

        //allows excess SAEA objects in pool.
        public const Int32 excessSaeaObjectsInPool = 1;

        //This number must be the same as the value on the client.
        //Tells what size the message prefix will be. Don't change this unless
        //you change the code, because 4 is the length of 32 bit integer, which
        //is what we are using as prefix.
        public const Int32 receivePrefixLength = 4;
        public const Int32 sendPrefixLength = 4;                

        public static Int32 mainTransMissionId = 10000;
        public static Int32 startingTid; //
        public static Int32 mainSessionId = 1000000000;

        public static List<DataHolder> listOfDataHolders;
                        
        //If you make this a positive value, it will simulate some delay on the
        //receive/send SAEA object after doing a receive operation.
        //That would be where you would do some work on the received data, 
        //before responding to the client.
        //This is in milliseconds. So a value of 1000 = 1 second delay.
        public static readonly Int32 msDelayAfterGettingMessage = -1;

        //This is for logging during testing.        
        //You can change the path in the TestFileWriter class if you need to.
        public static TestFileWriter testWriter;        

        // To keep a record of maximum number of simultaneous connections
        // that occur while the server is running. This can be limited by operating
        // system and hardware. It will not be higher than the value that you set
        // for maxNumberOfConnections.
        public static Int32 maxSimultaneousClientsThatWereConnected = 0;

        //These strings are just for console interaction.
        public const string checkString = "C";
        public const string closeString = "Z";
        public const string wpf = "T";
        public const string wpfNo = "F";
        public static string wpfTrueString = "";
        public static string wpfFalseString = "";

                
        static void Main(String[] args)
        {
            // Just used to calculate # of received transmissions at the end.
            startingTid = mainTransMissionId;

            // Create List<T> to hold data, unless we are running a long test, which
            // would create too much data to store in a list.
            if (runLongTest == false)
            {
                listOfDataHolders = new List<DataHolder>();
            }
            
            //Create a log file writer, so you can see the flow easily.
            //It can be printed. Makes it easier to figure out complex program flow.
            //The log StreamWriter uses a buffer. So it will only work right if you close
            //the server console properly at the end of the test.
            testWriter = new TestFileWriter();
            
            try
            {
                // Get endpoint for the listener.                
                IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, port);

                //This object holds a lot of settings that we pass from Main method
                //to the SocketListener. In a real app, you might want to read
                //these settings from a database or windows registry settings that
                //you would create.
                SocketListenerSettings theSocketListenerSettings = new SocketListenerSettings
        (maxNumberOfConnections, excessSaeaObjectsInPool, backlog, maxSimultaneousAcceptOps, receivePrefixLength, testBufferSize, sendPrefixLength, opsToPreAlloc, localEndPoint);
                
                //instantiate the SocketListener.
                SocketListener socketListener = new SocketListener(theSocketListenerSettings);
                
            }                           
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
            finally
            {   
                // close the stream for test file writing
                try
                {
                    testWriter.Close();
                }
                catch
                {
                    Console.WriteLine("Could not close log properly.");
                }
            }
        }
    }
}
