using System;

public struct Transaction
{
    public string AccountNumber { get; }
    public double Amount { get; }
    public Person Originator { get; }
    public DayTime Time { get; }

    public Transaction(string accountNumber, double amount, Person person)
    {
        AccountNumber = accountNumber;
        Amount = amount;
        Originator = person;
        Time = Utils.Time;
    }

    public override string ToString()
    {
        string transactionType = Amount >= 0 ? "Deposit" : "Withdraw";
        return $"{transactionType}: Account Number - {AccountNumber}, Person - {Originator.Name}, Amount - {Amount}, Time - {Time}";
    }
}

public struct DayTime
{
    private long minutes;

    public DayTime(long minutes)
    {
        this.minutes = minutes;
    }

    public static DayTime operator +(DayTime lhs, int minutes)
    {
        return new DayTime(lhs.minutes + minutes);
    }

    public override string ToString()
    {
        long year = minutes / (30 * 24 * 60 * 12);
        long month = (minutes % (30 * 24 * 60 * 12)) / (30 * 24 * 60);
        long day = ((minutes % (30 * 24 * 60 * 12)) % (30 * 24 * 60)) / (24 * 60);
        long hour = (((minutes % (30 * 24 * 60 * 12)) % (30 * 24 * 60)) % (24 * 60)) / 60;
        long remainingMinutes = (((minutes % (30 * 24 * 60 * 12)) % (30 * 24 * 60)) % (24 * 60)) % 60;
        return $"{year}-{month}-{day} {hour}:{remainingMinutes}";
    }
}

public interface ITransaction
{
    void Withdraw(double amount, Person person);
    void Deposit(double amount, Person person);
}

public enum ExceptionType
{
    ACCOUNT_DOES_NOT_EXIST,
    CREDIT_LIMIT_HAS_BEEN_EXCEEDED,
    NAME_NOT_ASSOCIATED_WITH_ACCOUNT,
    NO_OVERDRAFT,
    PASSWORD_INCORRECT,
    USER_DOES_NOT_EXIST,
    USER_NOT_LOGGED_IN
}

public enum AccountType
{
    Checking,
    Saving,
    Visa
}

//this class depends of the implementation of the following types:
//DayTime struct and AccountType enum
public static class Utils 
{
    static DayTime _time = new DayTime(1_048_000_000);
    static Random random = new Random();
    public static DayTime Time
    {
        get => _time += random.Next(1000);
    }
    public static DayTime Now
    {
        get => _time += 0;
    }

    public readonly static Dictionary<AccountType, string> ACCOUNT_TYPES = 
        new Dictionary<AccountType, string>
    {
        { AccountType.Checking , "CK" },
        { AccountType.Saving , "SV" },
        { AccountType.Visa , "VS" }
    };

}

public class Person
{
    private string password;
    public event EventHandler<LoginEventArgs> OnLogin;

    public string SIN { get; }
    public string Name { get; }
    public bool IsAuthenticated { get; private set; }

    public Person(string name, string sin)
    {
        Name = name;
        SIN = sin;
        password = SIN.Substring(0, 3);
        IsAuthenticated = false;
    }

    public void Login(string password)
    {
        if (password != this.password)
        {
            IsAuthenticated = false;
            OnLogin?.Invoke(this, new LoginEventArgs(Name, false));
            throw new AccountException(ExceptionType.PASSWORD_INCORRECT);
        }
        else
        {
            IsAuthenticated = true;
            OnLogin?.Invoke(this, new LoginEventArgs(Name, true));
        }
    }

    public void Logout()
    {
        IsAuthenticated = false;
    }

    public override string ToString()
    {
        return Name;
    }
}

public class LoginEventArgs : EventArgs
{
    public string Name { get; }
    public bool Success { get; }

    public LoginEventArgs(string name, bool success)
    {
        Name = name;
        Success = success;
    }
}

public class AccountException : Exception
{
    public ExceptionType Type { get; }

    public AccountException(ExceptionType type)
    {
        Type = type;
    }
}
