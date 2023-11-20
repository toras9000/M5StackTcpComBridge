using System;
using System.Collections;

namespace M5StackTcpComBridge;

public class ScrollConsole
{
    public ScrollConsole(int max = 10)
    {
        if (max <= 0) throw new ArgumentException(nameof(max));
        this.lines = new ArrayList();
        this.maxLines = max;
    }

    public ScrollConsole WriteLine(string text)
    {
        this.lines.Add(text);
        if (this.maxLines < this.lines.Count)
        {
            this.lines.Remove(0);
        }

        nanoFramework.M5Stack.Console.Clear();
        foreach (var line in this.lines)
        {
            nanoFramework.M5Stack.Console.WriteLine(line.ToString());
        }
        return this;
    }


    private ArrayList lines;
    private int maxLines;
}
