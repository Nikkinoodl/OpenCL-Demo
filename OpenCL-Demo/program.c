__kernel void CalcNumber(__global __const int* input1, __global __const int* input2, __global int* outputResult)
{
	int i = get_global_id(0);
	outputResult[i] = input1[i] * input2[i];
}