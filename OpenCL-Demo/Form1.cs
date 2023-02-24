using OpenTK;
using OpenTK.Compute;
using OpenTK.Compute.OpenCL;
using System.Security.Cryptography.Pkcs;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System;
using System.Runtime.InteropServices;
using System.ComponentModel;

namespace MeshGeneration_v2
{
    public partial class Form1 : Form
    {

        private static CLPlatform[]? platforms;
        private static CLDevice[]? devices;
        private static CLContext context;
        private static CLCommandQueue queue;
        private static CLProgram program;
        private static CLKernel kernel;

        private static bool validKernel;

        private static CLBuffer buffer1;
        private static CLBuffer buffer2;
        private static CLBuffer buffer3;

        private readonly nuint[] workSize = new nuint[] { 1 };
        private readonly nuint[] localSize = new nuint[] { 1 };

        private nint input1;
        private nint input2;
        private readonly nint outputResult;


        public Form1()
        {
            InitializeComponent();
        }

        unsafe private void CalcButton_Click(object sender, EventArgs e)
        {

            CLResultCode res;
            CLEvent clEvent;


            if (!validKernel)
            {
                res = CL.GetPlatformIds(out platforms);


                res = CL.GetDeviceIds(platforms[0], DeviceType.Gpu, out devices);
                CheckResult(res);

                //Insert conditional processing in the event there is more than one GPU
                //Find out how many devices are available
                foreach (var device in devices)
                {
                    Console.WriteLine(device.Handle.ToString());
                }

                //Create context (callback function and user data are not used (set to zero)).
                context = CL.CreateContext(IntPtr.Zero, 1, devices, IntPtr.Zero, IntPtr.Zero, out res);
                CheckResult(res);

                //Create command queue. Properties (settings) are set to zero, which uses default settings.
                queue = CL.CreateCommandQueueWithProperties(context, devices[0], IntPtr.Zero, out res);
                CheckResult(res);

                //Load C program and compile it
                string programSource = File.OpenText("..//..//..//program.c").ReadToEnd();
                program = CL.CreateProgramWithSource(context, programSource, out res);
                CheckResult(res);

                res = CL.BuildProgram(program, 1, devices, "", IntPtr.Zero, IntPtr.Zero);
                if (res != CLResultCode.Success)
                {
                    byte[] log;
                    CLResultCode res2 = CL.GetProgramBuildInfo(program, devices[0], ProgramBuildInfo.Log, out log);
                    if (res2 != CLResultCode.Success) { ViewError(res.ToString(), res.ToString()); throw new Exception("Error"); };
                    char[] logchar = new char[log.Length];
                    for (int i = 0; i < log.Length; i++)
                        logchar[i] = (char)log[i];

                    ViewError(new string(logchar), res.ToString());
                }

                //Create a kernel from the compiled program
                kernel = CL.CreateKernel(program, "CalcNumber", out res);
                CheckResult(res);

                validKernel = true;
            }

            //Size of the input and output in bytes (they are all the same in this case)
            nuint size1 = (UIntPtr)(sizeof(int) * 1);

            //Create pointers to the input and output variables.
            //They must be used only inside a 'fixed' area so that they will not be moved in memory.
            fixed (nint* ptr1 = &input1) fixed (nint* ptr2 = &input2) fixed (nint* ptr3 = &outputResult)
            {
                //Create buffers that transfer data to and from the GPU. Host pointer is used for specific memory
                //(eg. pinned memory) and is only used in conjunction with a specific memory flag
                buffer1 = CL.CreateBuffer(context, MemoryFlags.ReadOnly, size1, IntPtr.Zero, out res);
                CheckResult(res);

                buffer2 = CL.CreateBuffer(context, MemoryFlags.ReadOnly, size1, IntPtr.Zero, out res);
                CheckResult(res);

                buffer3 = CL.CreateBuffer(context, MemoryFlags.WriteOnly, size1, IntPtr.Zero, out res);
                CheckResult(res);

                //Send buffers to the GPU. Note the use of pointers.
                res = CL.EnqueueWriteBuffer(queue, buffer1, true, UIntPtr.Zero, size1, (nint)ptr1, 0, null, out clEvent);
                CheckResult(res);

                res = CL.EnqueueWriteBuffer(queue, buffer2, true, UIntPtr.Zero, size1, (nint)ptr2, 0, null, out clEvent);
                CheckResult(res);

                //Tell the kernel which buffers to use for each argument.
                res = CL.SetKernelArg(kernel, 0, buffer1);
                res = CL.SetKernelArg(kernel, 1, buffer2);
                res = CL.SetKernelArg(kernel, 2, buffer3);

                //Execute the kernel. Work dimension and work size are used to structure the grouping of calculations
                //in the GPU. Since we are doing just one calculation with simple integers, dimension and size are 1.
                res = CL.EnqueueNDRangeKernel(queue, kernel, 1, null, workSize, localSize, 0, null, out clEvent);
                CheckResult(res);

                //Get the output buffer back from the GPU
                res = CL.EnqueueReadBuffer(queue, buffer3, true, UIntPtr.Zero, size1, (nint)ptr3, 0, null, out clEvent);
                CheckResult(res);

                //Display result
                label1.Text = outputResult.ToString();
            
            }

            //Clean up
            res = CL.Finish(queue);
        }



        private void TextBox1_Validating(object sender, CancelEventArgs e)
        {
            //Parse text box contents and output to variable if a valid integer
            if (!nint.TryParse(textBox1.Text, out input1))
            {
                e.Cancel = true;
            }
                
        }
        private void TextBox2_Validating(object sender, CancelEventArgs e)
        {
            //Parse text box contents and output to variable if a valid integer
            if (!nint.TryParse(textBox2.Text, out input2))
            {
                e.Cancel = true;
            }
        }

        private void CheckResult(CLResultCode res)
        {
            if (res != CLResultCode.Success)
            {
                ViewError(res.ToString(), res.ToString());
                throw new Exception("Error");
            }
        }
        private void ViewError(string log, string type)
        {
            MessageBox.Show(log, "Error: " + type);
        }
    }
}