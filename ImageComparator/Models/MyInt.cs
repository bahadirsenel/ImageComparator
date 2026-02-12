using System;
using System.Runtime.CompilerServices;

namespace ImageComparator
{
    /// <summary>
    /// Thread-safe integer wrapper with change notification support.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class provides a thread-safe integer value that raises an event
    /// when the value changes. Used for progress tracking in multi-threaded scenarios.
    /// </para>
    /// <para>
    /// Thread Safety: All get and set operations on <see cref="Value"/> are synchronized
    /// using <see cref="MethodImplAttribute"/> with <see cref="MethodImplOptions.Synchronized"/>.
    /// </para>
    /// </remarks>
    public class MyInt
    {
        /// <summary>
        /// Represents the method that will handle the <see cref="OnChange"/> event.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">An <see cref="EventArgs"/> that contains the event data.</param>
        public delegate void MyIntEventHandler(object sender, EventArgs e);
        
        /// <summary>
        /// Occurs when the <see cref="Value"/> property changes.
        /// </summary>
        public event MyIntEventHandler OnChange;
        
        int myValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="MyInt"/> class with a value of 0.
        /// </summary>
        public MyInt()
        {
            Value = 0;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MyInt"/> class with the specified value.
        /// </summary>
        /// <param name="value">The initial value.</param>
        public MyInt(int value)
        {
            Value = value;
        }

        /// <summary>
        /// Gets or sets the integer value.
        /// </summary>
        /// <value>
        /// The current integer value. Both get and set operations are thread-safe.
        /// </value>
        /// <remarks>
        /// The <see cref="OnChange"/> event is raised only when the new value differs from the current value.
        /// </remarks>
        public int Value {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get {
                return myValue;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set {
                if (value != myValue)
                {
                    myValue = value;
                    onChange(EventArgs.Empty);
                }
            }
        }

        /// <summary>
        /// Atomically sets the value to the maximum of the current value and the specified value.
        /// </summary>
        /// <param name="value">The value to compare with the current value.</param>
        /// <remarks>
        /// <para>
        /// This method ensures that the value can only increase (monotonic updates).
        /// The read-compare-write operation is performed atomically under synchronization.
        /// </para>
        /// <para>
        /// Used in multi-threaded scenarios where multiple threads may update progress
        /// out of order, but the displayed value should never decrease.
        /// </para>
        /// </remarks>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void SetMaximum(int value)
        {
            if (value > myValue)
            {
                myValue = value;
                onChange(EventArgs.Empty);
            }
        }

        /// <summary>
        /// Raises the <see cref="OnChange"/> event.
        /// </summary>
        /// <param name="e">An <see cref="EventArgs"/> that contains the event data.</param>
        protected virtual void onChange(EventArgs e)
        {
            OnChange?.Invoke(this, e);
        }
    }
}