using System;

namespace PhysicsCSAlevlProject;

class MyStack<T>
{
    private T[] _stackArray;
    private int _top;

    public int Count => _top + 1;

    public MyStack()
    {
        _stackArray = new T[100];
        _top = -1;
    }

    public void Push(T item)
    {
        if (_top == _stackArray.Length - 1)
        {
            Resize();
        }
        _stackArray[++_top] = item;
    }

    private void Resize()
    {
        int newSize = _stackArray.Length * 2;
        T[] newArray = new T[newSize];
        Array.Copy(_stackArray, newArray, _stackArray.Length);
        _stackArray = newArray;
    }

    public T Pop()
    {
        if (_top < 0)
            throw new InvalidOperationException("Stack is empty.");
        return _stackArray[_top--];
    }

    public T Peek()
    {
        if (_top < 0)
            throw new InvalidOperationException("Stack is empty.");
        return _stackArray[_top];
    }

    public bool IsEmpty()
    {
        return _top < 0;
    }

    public void Clear()
    {
        _top = -1;
    }
}
