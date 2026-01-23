using System;
using System.Runtime.CompilerServices;

namespace ImageComparator2 {
    
    public class MyInt {

        public delegate void MyIntEventHandler(object sender, EventArgs e);
        public event MyIntEventHandler OnChange;
        int myValue;

        public MyInt() {

            Value = 0;
        }

        public MyInt(int value) {

            Value = value;
        }

        public int Value {

            [MethodImpl(MethodImplOptions.Synchronized)]
            get {
                return myValue;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set {
                if (value != myValue) {
                    myValue = value;
                    onChange(EventArgs.Empty);
                }
            }
        }

        protected virtual void onChange(EventArgs e) {

            if (OnChange != null) {
                OnChange(this, e);
            }
        }
    }
}
