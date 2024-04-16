using System;

namespace honooru.Models.App {

    public class UserSetting {

        public ulong AccountID { get; set; }

        public string Name { get; set; } = "";

        public UserSettingType Type { get; set; } = UserSettingType.DEFAULT;

        private string _Value = "";
        public string Value {
            get { return _Value; }
            set { 
                if (Type == UserSettingType.BOOLEAN) {
                    if (bool.TryParse(value, out _) == false) {
                        throw new FormatException($"could not convert {value} to a boolean");
                    }
                } else if (Type == UserSettingType.INTEGER) {
                    if (int.TryParse(value, out _) == false) {
                        throw new FormatException($"could not convert {value} to a boolean");
                    }
                } else if (Type == UserSettingType.DECIMAL) {
                    if (decimal.TryParse(value, out _) == false) {
                        throw new FormatException($"could not convert {value} to a decimal");
                    }
                } else if (Type == UserSettingType.STRING) {
                    // do nothing
                } else {
                    throw new Exception($"unchecked type of setting, Type={Type}");
                }

                _Value = value;
            }
        }

        public bool AsBoolean() {
            CheckType(UserSettingType.BOOLEAN);
            return Convert.ToBoolean(Value);
        }

        public int AsInteger() {
            CheckType(UserSettingType.INTEGER);
            return Convert.ToInt32(Value);
        }

        public decimal AsDecimal() {
            CheckType(UserSettingType.DECIMAL);
            return Convert.ToDecimal(Value);
        }

        public string AsString() {
            CheckType(UserSettingType.STRING);
            return Value;
        }

        private void CheckType(UserSettingType type) {
            if (this.Type != type) {
                throw new Exception($"the type of {nameof(UserSetting)} is {this.Type}, needed {type} [AccountID={AccountID}] [Name={Name}]");
            }
        }

    }

    public class UserSettingValue {

        public UserSettingType Type { get; set; } = UserSettingType.DEFAULT;

        public string Value { get; set; } = "";

        public static implicit operator UserSettingValue(string s) => new() {
            Type = UserSettingType.STRING,
            Value = s
        };

        public static implicit operator UserSettingValue(bool b) => new() {
            Type = UserSettingType.BOOLEAN,
            Value = b.ToString() 
        };

        public static implicit operator UserSettingValue(int i) => new() {
            Type = UserSettingType.INTEGER,
            Value = i.ToString()
        };

        public static implicit operator UserSettingValue(decimal d) => new() {
            Type = UserSettingType.DEFAULT,
            Value = d.ToString()
        };

        public override string ToString() {
            return Value;
        }

    }

    public enum UserSettingType {

        DEFAULT,

        BOOLEAN,

        INTEGER,

        DECIMAL,

        STRING,

    }

}
