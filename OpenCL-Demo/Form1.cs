using OpenTK.Compute.OpenCL;
using System.ComponentModel;

namespace OpenCLDemo
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

        // We use an array for the input and output variables, otherwise we must use pointers for enqueuing the 
        //CL buffers and this will make the code more complex.
        private int[] input1 = new int[1];
        private int[] input2 = new int[1];
        private int[] outputResult = new int[1];

        //Since we will be working with a single integer values, the work size and local size which are used to structure
        //computing space on the GPU are minimal and the sizes correspond to the size of the input/output arrays above.
        private readonly nuint[] workSize = new nuint[] { 1 };
        private readonly nuint[] localSize = new nuint[] { 1 };

        public Form1()
        {
            InitializeComponent();
        }

        private void CalcButton_Click(object sender, EventArgs e)
        {

            CLResultCode res;
            CLEvent clEvent;


            if (!validKernel)
            {
                res = CL.GetPlatformIds(out platforms);

                res = CL.GetDeviceIds(platforms[0], DeviceType.Gpu, out devices);
                CheckResult(res);

                //Create context (callback function and user data are not used (set to zero)).
                context = CL.CreateContext(IntPtr.Zero, 1, devices, IntPtr.Zero, IntPtr.Zero, out res);
                CheckResult(res);

                //Create command queue. We will use the first device found.
                //Properties (settings) are set to zero, which uses default settings.
                queue = CL.CreateCommandQueueWithProperties(context, devices[0], IntPtr.Zero, out res);
                CheckResult(res);

                //Load C program and compile it
                string programSource = File.OpenText("..//..//..//program.c").ReadToEnd();
                program = CL.CreateProgramWithSource(context, programSource, out res);
                CheckResult(res);

                res = CL.BuildProgram(program, 1, devices, "", IntPtr.Zero, IntPtr.Zero);
                if (res != CLResultCode.Success)
                {
                    CLResultCode res2 = CL.GetProgramBuildInfo(program, devices[0], ProgramBuildInfo.Log, out byte[] log);
                    CheckResult(res2);
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
            nuint size1 = sizeof(int) * 1;

            //Create buffers that transfer data to and from the GPU.
            // input variable and output variable must be passed in its own buffer.
            //Host pointer is used for specific memory
            //(eg. pinned memory) and is only used in conjunction with a specific memory flag.

            //Input buffer
            buffer1 = CL.CreateBuffer(context, MemoryFlags.ReadOnly, size1, IntPtr.Zero, out res);
            CheckResult(res);

            //Input buffer
            buffer2 = CL.CreateBuffer(context, MemoryFlags.ReadOnly, size1, IntPtr.Zero, out res);
            CheckResult(res);

            //Output buffer
            buffer3 = CL.CreateBuffer(context, MemoryFlags.WriteOnly, size1, IntPtr.Zero, out res);
            CheckResult(res);


            //Send buffers to the GPU.
            res = CL.EnqueueWriteBuffer(queue, buffer1, true, UIntPtr.Zero, input1, null, out clEvent);
            CheckResult(res);

            res = CL.EnqueueWriteBuffer(queue, buffer2, true, UIntPtr.Zero, input2, null, out clEvent);
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
            res = CL.EnqueueReadBuffer(queue, buffer3, true, UIntPtr.Zero, outputResult, null, out clEvent);
            CheckResult(res);

            //Display result
            label1.Text = outputResult[0].ToString();


            //Clean up
            res = CL.Finish(queue);
        }

        private void TextBox1_Validating(object sender, CancelEventArgs e)
        {
            //Parse text box contents and output to variable if a valid integer
            if (!int.TryParse(textBox1.Text, out input1[0]))
            {
                e.Cancel = true;
            }
        }

        private void TextBox2_Validating(object sender, CancelEventArgs e)
        {
            //Parse text box contents and output to variable if a valid integer
            if (!int.TryParse(textBox2.Text, out input2[0]))
            {
                e.Cancel = true;
            }
        }

        private static void CheckResult(CLResultCode res)
        {
            if (res != CLResultCode.Success)
            {
                ViewError(res.ToString(), res.ToString());
                throw new Exception("Error");
            }
        }
        private static void ViewError(string log, string type)
        {
            MessageBox.Show(log, "Error: " + type);
        }
    }
}