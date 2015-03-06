using System;
using System.Collections;
using System.Collections.Generic;

namespace Kiezel
{
    internal class EnumeratorProxy : IEnumerator
    {
        internal IEnumerator Iter;

        internal EnumeratorProxy( IEnumerator iter )
        {
            Iter = iter;
        }

        public object Current
        {
            get
            {
                return Iter.Current;
            }
        }

        public bool MoveNext()
        {
            return Iter.MoveNext();
        }

        public void Reset()
        {
            Iter.Reset();
        }
    }

    internal class UnisonEnumerator : IEnumerable<Vector>
    {
        private IEnumerable[] sequences;

        public UnisonEnumerator( IEnumerable[] sequences )
        {
            this.sequences = sequences ?? new IEnumerable[ 0 ];
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator<Vector> IEnumerable<Vector>.GetEnumerator()
        {
            List<IEnumerator> iterators = new List<IEnumerator>();

            foreach ( object seq in sequences )
            {
                if ( seq is IEnumerable )
                {
                    iterators.Add( ( ( IEnumerable ) seq ).GetEnumerator() );
                }
                else
                {
                    iterators.Add( null );
                }
            }

            while ( true )
            {
                var data = new Vector();
                int count = 0;

                for ( int i = 0; i < iterators.Count; ++i )
                {
                    if ( iterators[ i ] == null )
                    {
                        break;
                    }
                    else if ( iterators[ i ].MoveNext() )
                    {
                        ++count;
                        data.Add( iterators[ i ].Current );
                    }
                    else
                    {
                        break;
                    }
                }

                if ( count != 0 && count == iterators.Count )
                {
                    // full set
                    yield return data;
                }
                else
                {
                    break;
                }
            }
        }
    }

    
}