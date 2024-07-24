using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace KrnlSvc
{
    class Program
    {
        // Define constants
        private const uint SC_MANAGER_CREATE_SERVICE = 0x0002;
        private const uint SERVICE_KERNEL_DRIVER = 0x00000001;
        private const uint SERVICE_DEMAND_START = 0x00000003;
        private const uint SERVICE_ERROR_NORMAL = 0x00000001;
        private const uint SERVICE_ALL_ACCESS = 0xF01FF;
        private const uint SERVICE_QUERY_STATUS = 0x0004;
        private const uint SERVICE_START = 0x0010;
        private const uint SERVICE_STOP = 0x0020;
        private const uint SERVICE_RUNNING = 0x00000004;
        private const uint SERVICE_CONTROL_STOP = 0x00000001;
        private const uint SERVICE_QUERY_CONFIG = 0x00000001;

        // Define the necessary structs
        [StructLayout(LayoutKind.Sequential)]
        private struct SERVICE_STATUS
        {
            public uint dwServiceType;
            public uint dwCurrentState;
            public uint dwControlsAccepted;
            public uint dwWin32ExitCode;
            public uint dwServiceSpecificExitCode;
            public uint dwCheckPoint;
            public uint dwWaitHint;
        }

        // Define the necessary P/Invoke methods
        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern IntPtr OpenSCManager(string lpMachineName, string lpDatabaseName, uint dwDesiredAccess);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr CreateService(
            IntPtr hSCManager,
            string lpServiceName,
            string lpDisplayName,
            uint dwDesiredAccess,
            uint dwServiceType,
            uint dwStartType,
            uint dwErrorControl,
            string lpBinaryPathName,
            string lpLoadOrderGroup,
            IntPtr lpdwTagId,
            string lpDependencies,
            string lpServiceStartName,
            string lpPassword
            );

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern IntPtr OpenService(IntPtr hSCManager, string lpServiceName, uint dwDesiredAccess);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool QueryServiceStatus(IntPtr hService, out SERVICE_STATUS lpServiceStatus);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool StartService(IntPtr hService, int dwNumServiceArgs, string[] lpServiceArgVectors);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool CloseServiceHandle(IntPtr hSCObject);

        [DllImport("kernel32.dll")]
        private static extern uint GetLastError();

        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("[*] Usage: <program> <service_name> <driver_path>");
                return;
            }

            string serviceName = args[0];
            string driverPath = args[1];

            IntPtr scmHandle = OpenSCManager(null, null, SC_MANAGER_CREATE_SERVICE);

            if (scmHandle == IntPtr.Zero)
            {
                Console.WriteLine("[-] Failed to open SCM. Error: " + GetLastError());
                return;
            }
            else
            {
                Console.WriteLine("[+] Successfully opened SCM.");
            }

            IntPtr serviceHandle = OpenService(scmHandle, serviceName, SERVICE_QUERY_STATUS | SERVICE_START);

            if (serviceHandle == IntPtr.Zero)
            {
                serviceHandle = CreateService(
                    scmHandle,
                    serviceName,
                    serviceName,
                    SERVICE_ALL_ACCESS,
                    SERVICE_KERNEL_DRIVER,
                    SERVICE_DEMAND_START,
                    SERVICE_ERROR_NORMAL,
                    driverPath,
                    null,
                    IntPtr.Zero,
                    null,
                    null,
                    null);

                if (serviceHandle == IntPtr.Zero)
                {
                    Console.WriteLine("[-] Failed to create service. Error: " + GetLastError());
                    CloseServiceHandle(scmHandle);
                    return;
                }

                Console.WriteLine("[+] Service created successfully.");
            }
            else
            {
                Console.WriteLine("[*] Service already exists.");
            }

            SERVICE_STATUS serviceStatus;
            if (QueryServiceStatus(serviceHandle, out serviceStatus))
            {
                if (serviceStatus.dwCurrentState == SERVICE_RUNNING)
                {
                    Console.WriteLine("[*] Service is already running.");
                }
                else
                {
                    if (StartService(serviceHandle, 0, null))
                    {
                        Console.WriteLine("[+] Service started successfully.");
                    }
                    else
                    {
                        Console.WriteLine("[-] Failed to start service. Error: " + GetLastError());
                    }
                }
            }
            else
            {
                Console.WriteLine("[-] Failed to query service status. Error: " + GetLastError());
            }

            CloseServiceHandle(serviceHandle);
            CloseServiceHandle(scmHandle);
        }
    }
}
