using System.Collections.Generic;
using UnityEngine;

public class HeapManager : MonoBehaviour
{
    public List<int> heap = new List<int>();

    public void GenerateRandomHeap(int count)
    {
        heap.Clear();

        for (int i = 0; i < count; i++)
        {
            heap.Add(Random.Range(10, 99));
        }
    }

    public void BuildHeap()
    {
        for (int i = heap.Count / 2 - 1; i >= 0; i--)
        {
            Heapify(i, heap.Count);
        }
    }

    void Heapify(int i, int heapSize)
    {
        int largest = i;
        int left = 2 * i + 1;
        int right = 2 * i + 2;

        if (left < heapSize && heap[left] > heap[largest])
            largest = left;

        if (right < heapSize && heap[right] > heap[largest])
            largest = right;

        if (largest != i)
        {
            int temp = heap[i];
            heap[i] = heap[largest];
            heap[largest] = temp;

            Heapify(largest, heapSize);
        }
    }

    public int ExtractMax()
    {
        if (heap.Count == 0)
            return -1;

        int max = heap[0];

        heap[0] = heap[heap.Count - 1];
        heap.RemoveAt(heap.Count - 1);

        Heapify(0, heap.Count);

        return max;
    }
}
