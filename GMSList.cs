using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeltaruneSaveConverter
{
    enum GMSListItemType
    {
        GMSListReal,    // 0
        GMSListString,   // 1
        GMSListArray,    // 2
        GMSListInt32,    // 3
        GMSListUndefined,// 4-8
        GMSListBool,     // 9
        GMSListInt64,    // 10
        GMSListReference,// 11
        GMSListUnused,   // 12
        GMSListVector,   // 13
        GMSListStruct    // 14
    }

    class GMSListItem
    {
        public GMSListItemType type;
    }

    class GMSListStringItem : GMSListItem
    {
        public string stringValue;
        public GMSListStringItem(string value)
        {
            type = GMSListItemType.GMSListString;
            stringValue = value;
        }
        public override string ToString()
        {
            return stringValue;
        }
    }

    class GMSListRealItem : GMSListItem
    {
        public double realValue;
        public GMSListRealItem(double value)
        {
            type = GMSListItemType.GMSListReal;
            realValue = value;
        }
        public override string ToString()
        {
            return realValue.ToString();
        }
    }

    class GMSListInt32Item : GMSListItem
    {
        public int intValue;
        public GMSListInt32Item(int value)
        {
            type = GMSListItemType.GMSListInt32;
            intValue = value;
        }
        public override string ToString()
        {
            return intValue.ToString();
        }
    }

    class GMSListBoolItem : GMSListItem
    {
        public bool boolValue;
        public GMSListBoolItem(bool value)
        {
            type = GMSListItemType.GMSListBool;
            boolValue = value;
        }
        public override string ToString()
        {
            return boolValue ? "1" : "0";
        }
    }

    class GMSListVectorItem : GMSListItem
    {
        public double x;
        public double y;
        public double z;

        public GMSListVectorItem(double x, double y, double z)
        {
            type = GMSListItemType.GMSListVector;
            this.x = x;
            this.y = y;
            this.z = z;
        }
        public override string ToString()
        {
            return $"{x},{y},{z}";
        }
    }

    class GMSListDecoder
    {
        private byte[] rawlist;
        private List<GMSListItem> list;

        public GMSListDecoder(string listhex)
        {
            if (!listhex.StartsWith("2E010000") && !listhex.StartsWith("2F010000"))
            {
                throw new Exception("String was passed to GMSListDecoder that is not a ds_list.");
            }
            else if (listhex.Length % 2 != 0)
            {
                throw new Exception("String was passed to GMSListDecoder that is not an even length.");
            }

            rawlist = new byte[listhex.Length / 2];
            for (int i = 0; i < listhex.Length; i += 2) 
                rawlist[i / 2] = Convert.ToByte(listhex.Substring(i, 2), 16);
            
            list = new List<GMSListItem>();

            for (int i = 8; i < rawlist.Length;)
            {
                int typeValue = BitConverter.ToInt32(rawlist, i);
                i += 4;
                
                if (typeValue == (int)GMSListItemType.GMSListReal)
                {
                    double value = BitConverter.ToDouble(rawlist, i);
                    i += 8;
                    list.Add(new GMSListRealItem(value));
                }
                else if (typeValue == (int)GMSListItemType.GMSListString)
                {
                    int stringLength = BitConverter.ToInt32(rawlist, i);
                    i += 4;
                    string stringValue = "";
                    if (stringLength > 0)
                        stringValue = Encoding.UTF8.GetString(rawlist, i, stringLength);
                    list.Add(new GMSListStringItem(stringValue));
                    i += stringLength;
                }
                else if (typeValue == (int)GMSListItemType.GMSListInt32)
                {
                    int value = BitConverter.ToInt32(rawlist, i);
                    i += 4;
                    list.Add(new GMSListInt32Item(value));
                }
                else if (typeValue == (int)GMSListItemType.GMSListBool)
                {
                    int value = BitConverter.ToInt32(rawlist, i);
                    i += 4;
                    list.Add(new GMSListBoolItem(value != 0));
                }
                else if (typeValue == (int)GMSListItemType.GMSListVector) // Tipo 13: Vector
                {
                    double x = BitConverter.ToDouble(rawlist, i);
                    i += 8;
                    double y = BitConverter.ToDouble(rawlist, i);
                    i += 8;
                    double z = BitConverter.ToDouble(rawlist, i);
                    i += 8;
                    list.Add(new GMSListVectorItem(x, y, z));
                }
                else
                {
                    // Manejar otros tipos como placeholder
                    int bytesToSkip = GetSizeForUnknownType(typeValue);
                    i += bytesToSkip;
                    list.Add(new GMSListRealItem(0));
                }
            }
        }

        private int GetSizeForUnknownType(int typeValue)
        {
            // Lógica para determinar tamaño basado en el tipo
            switch (typeValue)
            {
                case 2:  // Array (no implementado)
                case 4:  // Undefined
                case 5:  // Undefined
                case 6:  // Undefined
                case 7:  // Undefined
                case 8:  // Undefined
                case 10: // Int64
                case 11: // Reference
                case 12: // Unused
                case 14: // Struct
                default:
                    return 8; // Tamaño por defecto (seguro)
            }
        }

        public int ListSize()
        {
            return list.Count;
        }

        public string GetString(int i)
        {
            if (i < 0 || i >= list.Count) return "";
            return list[i].ToString();
        }

        public double GetReal(int i)
        {
            if (i < 0 || i >= list.Count) return 0;

            if (list[i] is GMSListRealItem realItem)
                return realItem.realValue;
            
            if (list[i] is GMSListInt32Item intItem)
                return intItem.intValue;
            
            if (list[i] is GMSListBoolItem boolItem)
                return boolItem.boolValue ? 1.0 : 0.0;
            
            if (list[i] is GMSListVectorItem vectorItem)
                return vectorItem.x; // Devolver solo el primer componente
            
            return 0;
        }

        public void ToRealArray(ref double[] output, int length)
        {
            int elementsToCopy = Math.Min(length, list.Count);
            for (int i = 0; i < elementsToCopy; i++)
            {
                output[i] = GetReal(i);
            }
        }

        public void ToStringArray(ref string[] output, int length)
        {
            int elementsToCopy = Math.Min(length, list.Count);
            for (int i = 0; i < elementsToCopy; i++)
            {
                output[i] = GetString(i);
            }
        }
    }

    class GMSListEncoder
    {
        private byte[] rawlist;
        private List<GMSListItem> list;

        public GMSListEncoder()
        {
            list = new List<GMSListItem>();
        }

        public GMSListEncoder(IEnumerable<double> reallist)
        {
            list = new List<GMSListItem>();
            foreach(double item in reallist)
                list.Add(new GMSListRealItem(item));
        }

        public GMSListEncoder(IEnumerable<string> stringlist)
        {
            list = new List<GMSListItem>();
            foreach (string item in stringlist)
                list.Add(new GMSListStringItem(item));
        }

        public void AddReal(double real)
        {
            list.Add(new GMSListRealItem(real));
        }

        public void AddString(string stringVal)
        {
            list.Add(new GMSListStringItem(stringVal));
        }

        public void AddVector(double x, double y, double z)
        {
            list.Add(new GMSListVectorItem(x, y, z));
        }

        public byte[] GetRaw()
        {
            List<byte> temp = new();
            temp.AddRange(BitConverter.GetBytes(0x12E)); // Magic number
            temp.AddRange(BitConverter.GetBytes(list.Count));
            
            foreach (var item in list)
            {
                temp.AddRange(BitConverter.GetBytes((int)item.type));
                
                if (item is GMSListRealItem realItem)
                {
                    temp.AddRange(BitConverter.GetBytes(realItem.realValue));
                }
                else if (item is GMSListStringItem stringItem)
                {
                    byte[] stringBytes = Encoding.UTF8.GetBytes(stringItem.stringValue);
                    temp.AddRange(BitConverter.GetBytes(stringBytes.Length));
                    temp.AddRange(stringBytes);
                }
                else if (item is GMSListInt32Item intItem)
                {
                    temp.AddRange(BitConverter.GetBytes(intItem.intValue));
                }
                else if (item is GMSListBoolItem boolItem)
                {
                    temp.AddRange(BitConverter.GetBytes(boolItem.boolValue ? 1 : 0));
                }
                else if (item is GMSListVectorItem vectorItem)
                {
                    temp.AddRange(BitConverter.GetBytes(vectorItem.x));
                    temp.AddRange(BitConverter.GetBytes(vectorItem.y));
                    temp.AddRange(BitConverter.GetBytes(vectorItem.z));
                }
            }
            
            rawlist = temp.ToArray();
            return rawlist;
        }

        public string GetString()
        {
            GetRaw();
            return BitConverter.ToString(rawlist).Replace("-", "");
        }
    }
}