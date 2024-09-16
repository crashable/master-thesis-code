using Unity.Collections;
using Unity.Jobs;

namespace JobSystem
{
    internal struct AudioConversionJob : IJobParallelFor {
    private const float PCMToFloatFactor = 32768f; // Conversion factor to convert from 16-bit PCM to float
    [ReadOnly] public NativeArray<byte> Input;
    public NativeArray<float> Output;
    
    public void Execute(int index) {
        int byteIndex = index * 2;
        short sample = (short)(Input[byteIndex] | (Input[byteIndex + 1] << 8));
        Output[index] = sample / PCMToFloatFactor;   // Convert from 16-bit PCM to float [-1,1] range
    }
}
}