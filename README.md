# BufferSorter
This is a very simple tool for in-place sorting of ComputeBuffers of ints or uints on the GPU, meant for use in Unity. I wrote this to use with a basic GPU particle system I'm working on, which needs to always be sorted consistently between frames.

[This is based on the great work of nobnak, available here.](https://github.com/nobnak/GPUMergeSortForUnity) I simply wanted to make it a little more user friendly for my own needs. The fundamental sorting algorithm is their own work.

## My additions are:

* It now works in-place, changing the data in the given ComputeBuffer to be sorted.
* The data buffer no longer needs to have a power of two size.
* You can optionally sort in reverse.
* You can optionally sort only a subset of the data, the first of N. Note that this option completely disregards the content of the remainder of the buffer, so it could be considered 'destructive' in that the set of ints will no longer be the same.
* I've generally just hidden a lot of the extra bits and bobs away inside the class, making it very simple to use from the outside.

## How to Use

Like this:

![image](https://user-images.githubusercontent.com/18707147/126571298-38079350-4d3f-412f-8b92-2a69666b5f29.png)
 
 It couldn't be simpler, really. Reverse and sublist length are optional parameters.
