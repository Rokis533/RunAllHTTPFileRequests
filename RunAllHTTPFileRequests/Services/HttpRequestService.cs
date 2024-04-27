using EnvDTE80;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Diagnostics;
using System.ComponentModel;
using System.Windows.Controls;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using System.Windows.Forms;
using EnvDTE;

namespace RunAllHTTPFileRequests.Services
{
    public class HttpRequestService
    {
        private DTE2 _dte = ServiceProvider.GlobalProvider.GetService(typeof(SDTE)) as DTE2;

        public HttpRequestService()
        {
        }
        public async Task ClickSendRequestButtons()
        {
            try
            {
                var rootElement = AutomationElement.RootElement;

                // Find the main Visual Studio window
                var vsMainWindow = GetVisualStudioMainWindow();

                if (vsMainWindow == null)
                {
                    Console.WriteLine("Visual Studio main window not found.");
                    return;
                }

                var buttons = FindSendRequestAdornmentButtons(vsMainWindow);
                await InvokeButtons(buttons);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex.Message);
            }


        }
        public AutomationElement GetVisualStudioMainWindow()
        {
            IntPtr hWnd = _dte?.MainWindow?.HWnd ?? IntPtr.Zero;
            if (hWnd == IntPtr.Zero)
            {
                Console.WriteLine("Failed to obtain Visual Studio main window handle.");
                return null;
            }

            int processId = 0;
            GetWindowThreadProcessId(hWnd, out processId);
            if (processId == 0)
            {
                Console.WriteLine("Failed to obtain Visual Studio process ID.");
                return null;
            }

            var vsProcess = System.Diagnostics.Process.GetProcessById(processId);
            if (vsProcess == null)
            {
                Console.WriteLine("Failed to find Visual Studio process.");
                return null;
            }

            var vsRoot = AutomationElement.FromHandle(vsProcess.MainWindowHandle);
            return vsRoot;
        }

        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        static extern int GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);
        private AutomationElementCollection FindSendRequestAdornmentButtons(AutomationElement parent)
        {

            var elements = parent.FindAll(TreeScope.Descendants, new PropertyCondition(AutomationElement.ClassNameProperty, "SendRequestAdornment"));

            return elements;
        }

        private async Task InvokeButtons(AutomationElementCollection containers)
        {
            foreach (AutomationElement container in containers)
            {
                var buttonCondition = new AndCondition(
    new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Hyperlink),
    new PropertyCondition(AutomationElement.NameProperty, "Send request")
);

                var button = container.FindFirst(TreeScope.Descendants, buttonCondition);
                await Task.Delay(1000);
                if (!button.Current.IsEnabled)
                {
                    Console.WriteLine("Button is disabled, waiting for it to become enabled...");
                    await WaitForElementToBeEnabled(button);
                }

                if (button.TryGetCurrentPattern(InvokePattern.Pattern, out object pattern))
                {
                    var invokePattern = (InvokePattern)pattern;
                    try
                    {
                        invokePattern.Invoke();
                        Console.WriteLine("Button clicked: " + button.Current.Name);

                    }
                    catch (ElementNotEnabledException)
                    {
                        Console.WriteLine("Failed to click button; it was disabled at the moment of invocation.");
                    }
                }
                else
                {
                    Console.WriteLine("Invoke pattern not supported on this button.");
                }
            }
            await Task.Delay(1000);
        }

        private async Task WaitForElementToBeEnabled(AutomationElement element, int timeoutMilliseconds = 30000)
        {
            var stopwatch = Stopwatch.StartNew();
            while (!element.Current.IsEnabled)
            {
                if (stopwatch.ElapsedMilliseconds > timeoutMilliseconds)
                {
                    Console.WriteLine("Timed out waiting for the button to become enabled.");
                    return;
                }
                await Task.Delay(1000);
            }
        }



        public async Task<string> ExecuteHttpRequests(string fileContent)
        {
            await ClickSendRequestButtons();
            return "done";
        }
    }
}
