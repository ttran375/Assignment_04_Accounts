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

public class TransactionEventArgs : LoginEventArgs
{
    public double Amount { get; }

    public TransactionEventArgs(string personName, double amount, bool success) : base(personName, success)
    {
        Amount = amount;
    }
}

public static class Logger
{
    private static List<string> loginEvents = new List<string>();
    private static List<string> transactionEvents = new List<string>();

    public static void LoginHandler(object sender, EventArgs args)
    {
        LoginEventArgs loginArgs = args as LoginEventArgs;
        string logMessage = $"{loginArgs.Name} login attempt: {loginArgs.Success}, Time: {Utils.Now}";
        loginEvents.Add(logMessage);
    }

    public static void TransactionHandler(object sender, EventArgs args)
    {
        TransactionEventArgs transactionArgs = args as TransactionEventArgs;
        string logMessage = $"{transactionArgs.Name} {transactionArgs.Success} transaction: {transactionArgs.Amount}, Time: {Utils.Now}";
        transactionEvents.Add(logMessage);
    }

    public static void ShowLoginEvents()
    {
        Console.WriteLine($"Current Time: {Utils.Now}");
        Console.WriteLine("Login Events:");
        for (int i = 0; i < loginEvents.Count; i++)
        {
            Console.WriteLine($"{i + 1}. {loginEvents[i]}");
        }
    }

    public static void ShowTransactionEvents()
    {
        Console.WriteLine($"Current Time: {Utils.Now}");
        Console.WriteLine("Transaction Events:");
        for (int i = 0; i < transactionEvents.Count; i++)
        {
            Console.WriteLine($"{i + 1}. {transactionEvents[i]}");
        }
    }
}

public abstract class Account
{
    private static int LAST_NUMBER = 100_000;

    protected readonly List<Person> users;
    public readonly List<Transaction> transactions;

    public event EventHandler<TransactionEventArgs> OnTransaction;

    public string Number { get; }
    public double Balance { get; protected set; }
    public double LowestBalance { get; protected set; }

    public Account(string type, double balance)
    {
        Number = $"{type}-{++LAST_NUMBER}";
        Balance = balance;
        LowestBalance = balance;
        transactions = new List<Transaction>();
        users = new List<Person>();
    }

    public void Deposit(double amount, Person person)
    {
        Balance += amount;
        if (Balance < LowestBalance)
            LowestBalance = Balance;

        Transaction transaction = new Transaction(Number, amount, person);
        transactions.Add(transaction);

        OnTransaction?.Invoke(this, new TransactionEventArgs(person.Name, amount, true));
    }

    public void AddUser(Person person)
    {
        users.Add(person);
    }

    public bool IsUser(string name)
    {
        return users.Any(user => user.Name == name);
    }

    public abstract void PrepareMonthlyReport();

    protected void RaiseOnTransaction(TransactionEventArgs args)
    {
        OnTransaction?.Invoke(this, args);
    }

    public override string ToString()
    {
        string transactionDetails = string.Join("\n", transactions.Select((transaction, index) => $"{index + 1}. {transaction}"));
        string userNames = string.Join(", ", users.Select(user => user.Name));

        return $"Account Number: {Number}\nUsers: {userNames}\nBalance: {Balance}\nTransactions:\n{transactionDetails}";
    }
}

public class CheckingAccount : Account, ITransaction
{
    // Class variables
    public static readonly double COST_PER_TRANSACTION = 0.05;
    public static readonly double INTEREST_RATE = 0.005;

    // Instance variables
    private bool hasOverdraft;

    // Constructor
    public CheckingAccount(double balance = 0, bool hasOverdraft = false)
        : base(Utils.ACCOUNT_TYPES[AccountType.Checking], balance)
    {
        this.hasOverdraft = hasOverdraft;
    }

    // Override Deposit method to include transaction cost and event
    public new void Deposit(double amount, Person person)
    {
        base.Deposit(amount, person); // Call base class deposit method
                                      // In CheckingAccount methods where you want to raise the event
        RaiseOnTransaction(new TransactionEventArgs(person.Name, amount, true));

    }

    // Implement Withdraw method from ITransaction
    public void Withdraw(double amount, Person person)
    {
        if (!IsUser(person.Name))
        {
            // In CheckingAccount methods where you want to raise the event
            RaiseOnTransaction(new TransactionEventArgs(person.Name, amount, false));

            throw new AccountException(ExceptionType.NAME_NOT_ASSOCIATED_WITH_ACCOUNT);
        }
        else if (!person.IsAuthenticated)
        {
            // In CheckingAccount methods where you want to raise the event
            RaiseOnTransaction(new TransactionEventArgs(person.Name, amount, false));

            throw new AccountException(ExceptionType.USER_NOT_LOGGED_IN);
        }
        else if (amount > Balance && !hasOverdraft)
        {
            // In CheckingAccount methods where you want to raise the event
            RaiseOnTransaction(new TransactionEventArgs(person.Name, amount, false));

            throw new AccountException(ExceptionType.NO_OVERDRAFT);
        }
        else
        {
            // For withdrawal, deposit a negative amount
            base.Deposit(-amount, person); // This adjusts the balance
                                           // In CheckingAccount methods where you want to raise the event
            RaiseOnTransaction(new TransactionEventArgs(person.Name, -amount, true));

        }
    }

    // Override PrepareMonthlyReport to handle checking account specifics
    public override void PrepareMonthlyReport()
    {
        double serviceCharge = transactions.Count * COST_PER_TRANSACTION;
        double interest = (LowestBalance * INTEREST_RATE) / 12;
        Balance += interest - serviceCharge;
        transactions.Clear(); // Clear transactions after updating balance
    }
}

public class SavingAccount : Account, ITransaction
{
    // Class variables
    public static readonly double COST_PER_TRANSACTION = 0.5;
    public static readonly double INTEREST_RATE = 0.015;

    // Constructor
    public SavingAccount(double balance = 0)
        : base(Utils.ACCOUNT_TYPES[AccountType.Saving], balance)
    {
        // Note: hasOverdraft parameter removed as per the fields description
    }

    // Override Deposit method to include transaction cost and event
    public new void Deposit(double amount, Person person)
    {
        base.Deposit(amount, person); // Call base class deposit method
        RaiseOnTransaction(new TransactionEventArgs(person.Name, amount, true)); // Assuming RaiseOnTransaction is implemented in Account
    }

    // Implement Withdraw method from ITransaction
    public void Withdraw(double amount, Person person)
    {
        if (!IsUser(person.Name))
        {
            RaiseOnTransaction(new TransactionEventArgs(person.Name, amount, false));
            throw new AccountException(ExceptionType.NAME_NOT_ASSOCIATED_WITH_ACCOUNT);
        }
        else if (!person.IsAuthenticated)
        {
            RaiseOnTransaction(new TransactionEventArgs(person.Name, amount, false));
            throw new AccountException(ExceptionType.USER_NOT_LOGGED_IN);
        }
        else if (amount > Balance) // No overdraft for SavingAccount
        {
            RaiseOnTransaction(new TransactionEventArgs(person.Name, amount, false));
            throw new AccountException(ExceptionType.NO_OVERDRAFT);
        }
        else
        {
            base.Deposit(-amount, person); // Adjust balance by depositing a negative amount
            RaiseOnTransaction(new TransactionEventArgs(person.Name, -amount, true));
        }
    }

    // Override PrepareMonthlyReport to handle saving account specifics
    public override void PrepareMonthlyReport()
    {
        double serviceCharge = transactions.Count * COST_PER_TRANSACTION;
        double interest = (LowestBalance * INTEREST_RATE) / 12;
        Balance += interest - serviceCharge;
        transactions.Clear(); // Clear transactions after updating balance
    }
}

public class VisaAccount : Account, ITransaction
{
    // Instance variables
    private double creditLimit;

    // Class variable
    public static readonly double INTEREST_RATE = 0.1995;

    // Constructor
    public VisaAccount(double balance = 0, double creditLimit = 1200)
        : base(Utils.ACCOUNT_TYPES[AccountType.Visa], balance)
    {
        this.creditLimit = creditLimit;
    }

    // DoPayment method to handle credit payments
    public void DoPayment(double amount, Person person)
    {
        base.Deposit(amount, person); // Increase the balance (reduce debt)
        RaiseOnTransaction(new TransactionEventArgs(person.Name, amount, true)); // Assuming RaiseOnTransaction is implemented in Account
    }

    // DoPurchase method to handle purchases on the credit account
    public void DoPurchase(double amount, Person person)
    {
        if (!IsUser(person.Name))
        {
            RaiseOnTransaction(new TransactionEventArgs(person.Name, -amount, false));
            throw new AccountException(ExceptionType.NAME_NOT_ASSOCIATED_WITH_ACCOUNT);
        }
        else if (!person.IsAuthenticated)
        {
            RaiseOnTransaction(new TransactionEventArgs(person.Name, -amount, false));
            throw new AccountException(ExceptionType.USER_NOT_LOGGED_IN);
        }
        else if (Balance + amount > creditLimit) // Checks if purchase exceeds credit limit
        {
            RaiseOnTransaction(new TransactionEventArgs(person.Name, -amount, false));
            throw new AccountException(ExceptionType.CREDIT_LIMIT_HAS_BEEN_EXCEEDED);
        }
        else
        {
            base.Deposit(-amount, person); // Decrease the balance (increase debt)
            RaiseOnTransaction(new TransactionEventArgs(person.Name, -amount, true));
        }
    }

    // Override PrepareMonthlyReport to calculate and apply interest
    public override void PrepareMonthlyReport()
    {
        double interest = (LowestBalance * INTEREST_RATE) / 12;
        Balance -= interest; // Subtracting interest increases the debt
        transactions.Clear(); // Clear transactions after updating balance
    }

    public void Withdraw(double amount, Person person)
    {
        DoPurchase(amount, person);
    }

    public void Pay(double amount, Person person)
    {
        if (!IsUser(person.Name))
        {
            RaiseOnTransaction(new TransactionEventArgs(person.Name, amount, false));
            throw new AccountException(ExceptionType.NAME_NOT_ASSOCIATED_WITH_ACCOUNT);
        }
        else if (!person.IsAuthenticated)
        {
            RaiseOnTransaction(new TransactionEventArgs(person.Name, amount, false));
            throw new AccountException(ExceptionType.USER_NOT_LOGGED_IN);
        }
        else if (amount < 0)
        {
            RaiseOnTransaction(new TransactionEventArgs(person.Name, amount, false));
            throw new ArgumentException("Payment amount must be positive");
        }
        else
        {
            base.Deposit(-amount, person); // Decrease the balance (repay debt)
            RaiseOnTransaction(new TransactionEventArgs(person.Name, -amount, true));
        }
    }

    public void Purchase(double amount, Person person)
    {
        if (!IsUser(person.Name))
        {
            RaiseOnTransaction(new TransactionEventArgs(person.Name, -amount, false));
            throw new AccountException(ExceptionType.NAME_NOT_ASSOCIATED_WITH_ACCOUNT);
        }
        else if (!person.IsAuthenticated)
        {
            RaiseOnTransaction(new TransactionEventArgs(person.Name, -amount, false));
            throw new AccountException(ExceptionType.USER_NOT_LOGGED_IN);
        }
        else if (amount < 0)
        {
            RaiseOnTransaction(new TransactionEventArgs(person.Name, -amount, false));
            throw new ArgumentException("Purchase amount must be positive");
        }
        else if (Balance + amount > creditLimit)
        {
            RaiseOnTransaction(new TransactionEventArgs(person.Name, -amount, false));
            throw new AccountException(ExceptionType.CREDIT_LIMIT_HAS_BEEN_EXCEEDED);
        }
        else
        {
            base.Deposit(-amount, person); // Decrease the balance (increase debt)
            RaiseOnTransaction(new TransactionEventArgs(person.Name, -amount, true));
        }
    }

}

public static class Bank
{
    public static readonly Dictionary<string, Account> ACCOUNTS = new Dictionary<string, Account>();
    public static readonly Dictionary<string, Person> USERS = new Dictionary<string, Person>();

    static Bank()
    {
        //initialize the USERS collection

        AddPerson("Narendra", "1234-5678"); //0

        AddPerson("Ilia", "2345-6789"); //1

        AddPerson("Mehrdad", "3456-7890"); //2

        AddPerson("Vijay", "4567-8901"); //3

        AddPerson("Arben", "5678-9012"); //4

        AddPerson("Patrick", "6789-0123"); //5

        AddPerson("Yin", "7890-1234"); //6

        AddPerson("Hao", "8901-2345"); //7

        AddPerson("Jake", "9012-3456"); //8

        AddPerson("Mayy", "1224-5678"); //9

        AddPerson("Nicoletta", "2344-6789"); //10

        //initialize the ACCOUNTS collection

        AddAccount(new VisaAccount()); //VS-100000

        AddAccount(new VisaAccount(150, -500)); //VS-100001

        AddAccount(new SavingAccount(5000)); //SV-100002

        AddAccount(new SavingAccount()); //SV-100003

        AddAccount(new CheckingAccount(2000)); //CK-100004

        AddAccount(new CheckingAccount(1500, true));//CK-100005

        AddAccount(new VisaAccount(50, -550)); //VS-100006

        AddAccount(new SavingAccount(1000)); //SV-100007

        //associate users with accounts

        string number = "VS-100000";

        AddUserToAccount(number, "Narendra");

        AddUserToAccount(number, "Ilia");

        AddUserToAccount(number, "Mehrdad");

        number = "VS-100001";

        AddUserToAccount(number, "Vijay");

        AddUserToAccount(number, "Arben");

        AddUserToAccount(number, "Patrick");

        number = "SV-100002";

        AddUserToAccount(number, "Yin");

        AddUserToAccount(number, "Hao");

        AddUserToAccount(number, "Jake");

        number = "SV-100003";

        AddUserToAccount(number, "Mayy");

        AddUserToAccount(number, "Nicoletta");

        number = "CK-100004";

        AddUserToAccount(number, "Mehrdad");

        AddUserToAccount(number, "Arben");

        AddUserToAccount(number, "Yin");

        number = "CK-100005";

        AddUserToAccount(number, "Jake");

        AddUserToAccount(number, "Nicoletta");

        number = "VS-100006";

        AddUserToAccount(number, "Ilia");

        AddUserToAccount(number, "Vijay");

        number = "SV-100007";

        AddUserToAccount(number, "Patrick");

        AddUserToAccount(number, "Hao");
    }

    public static void AddPerson(string name, string sin)
    {
        Person person = new Person(name, sin);
        person.OnLogin += Logger.LoginHandler;
        USERS.Add(name, person);
    }

    public static void AddAccount(Account account)
    {
        account.OnTransaction += Logger.TransactionHandler;
        ACCOUNTS.Add(account.Number, account);
    }

    public static void AddUserToAccount(string number, string name)
    {
        if (ACCOUNTS.ContainsKey(number) && USERS.ContainsKey(name))
        {
            Account account = ACCOUNTS[number];
            Person person = USERS[name];
            account.AddUser(person);
        }
        else
        {
            throw new AccountException(ExceptionType.ACCOUNT_DOES_NOT_EXIST);
        }
    }

    public static void PrintAccounts()
    {
        foreach (var account in ACCOUNTS)
        {
            Console.WriteLine(account.Value);
        }
    }

    public static void PrintPersons()
    {
        foreach (var user in USERS)
        {
            Console.WriteLine(user.Value);
        }
    }

    public static Person GetPerson(string name)
    {
        if (USERS.ContainsKey(name))
        {
            return USERS[name];
        }
        else
        {
            throw new AccountException(ExceptionType.USER_DOES_NOT_EXIST);
        }
    }

    public static Account GetAccount(string number)
    {
        if (ACCOUNTS.ContainsKey(number))
        {
            return ACCOUNTS[number];
        }
        else
        {
            throw new AccountException(ExceptionType.ACCOUNT_DOES_NOT_EXIST);
        }
    }

    public static List<Transaction> GetAllTransactions()
    {
        List<Transaction> allTransactions = new List<Transaction>();
        foreach (var account in ACCOUNTS)
        {
            allTransactions.AddRange(account.Value.transactions);
        }
        return allTransactions;
    }


}

class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("\nAll acounts:");

        Bank.PrintAccounts();

        Console.WriteLine("\nAll Users:");

        Bank.PrintPersons();

        Person p0, p1, p2, p3, p4, p5, p6, p7, p8, p9, p10;

        p0 = Bank.GetPerson("Narendra");

        p1 = Bank.GetPerson("Ilia");

        p2 = Bank.GetPerson("Mehrdad");

        p3 = Bank.GetPerson("Vijay");

        p4 = Bank.GetPerson("Arben");

        p5 = Bank.GetPerson("Patrick");

        p6 = Bank.GetPerson("Yin");

        p7 = Bank.GetPerson("Hao");

        p8 = Bank.GetPerson("Jake");

        p9 = Bank.GetPerson("Mayy");

        p10 = Bank.GetPerson("Nicoletta");

        p0.Login("123"); p1.Login("234");

        p2.Login("345"); p3.Login("456");

        p4.Login("567"); p5.Login("678");

        p6.Login("789"); p7.Login("890");

        p10.Login("234"); p8.Login("901");

        //a visa account

        VisaAccount a = Bank.GetAccount("VS-100000") as VisaAccount;

        a.Pay(1500, p0);

        a.Purchase(200, p1);

        a.Purchase(25, p2);

        a.Purchase(15, p0);

        a.Purchase(39, p1);

        a.Pay(400, p0);

        Console.WriteLine(a);

        a = Bank.GetAccount("VS-100001") as VisaAccount;

        a.Pay(500, p0);

        a.Purchase(25, p3);

        a.Purchase(20, p4);

        a.Purchase(15, p5);

        Console.WriteLine(a);

        //a saving account

        SavingAccount b = Bank.GetAccount("SV-100002") as SavingAccount;

        b.Withdraw(300, p6);

        b.Withdraw(32.90, p6);

        b.Withdraw(50, p7);

        b.Withdraw(111.11, p8);

        Console.WriteLine(b);

        b = Bank.GetAccount("SV-100003") as SavingAccount;

        b.Deposit(300, p3); //ok even though p3 is not a holder

        b.Deposit(32.90, p2);

        b.Deposit(50, p5);

        b.Withdraw(111.11, p10);

        Console.WriteLine(b);

        //a checking account

        CheckingAccount c = Bank.GetAccount("CK-100004") as CheckingAccount;

        c.Deposit(33.33, p7);

        c.Deposit(40.44, p7);

        c.Withdraw(150, p2);

        c.Withdraw(200, p4);

        c.Withdraw(645, p6);

        c.Withdraw(350, p6);

        Console.WriteLine(c);

        c = Bank.GetAccount("CK-100005") as CheckingAccount;

        c.Deposit(33.33, p8);

        c.Deposit(40.44, p7);

        c.Withdraw(450, p10);

        c.Withdraw(500, p8);

        c.Withdraw(645, p10);

        c.Withdraw(850, p10);

        Console.WriteLine(c);

        a = Bank.GetAccount("VS-100006") as VisaAccount;

        a.Pay(700, p0);

        a.Purchase(20, p3);

        a.Purchase(10, p1);

        a.Purchase(15, p1);

        Console.WriteLine(a);

        b = Bank.GetAccount("SV-100007") as SavingAccount;

        b.Deposit(300, p3); //ok even though p3 is not a holder

        b.Deposit(32.90, p2);

        b.Deposit(50, p5);

        b.Withdraw(111.11, p7);

        Console.WriteLine(b);

        Console.WriteLine("\n\nExceptions:");

        //The following will cause exception

        try

        {

            p8.Login("911"); //incorrect password

        }

        catch (AccountException e) { Console.WriteLine(e.Message); }

        try

        {

            p3.Logout();

            a.Purchase(12.5, p3); //exception user is not logged in

        }

        catch (AccountException e) { Console.WriteLine(e.Message); }

        try

        {

            a.Purchase(12.5, p0); //user is not associated with this account

        }

        catch (AccountException e) { Console.WriteLine(e.Message); }

        try

        {

            a.Purchase(5825, p4); //credit limit exceeded

        }

        catch (AccountException e) { Console.WriteLine(e.Message); }

        try

        {

            c.Withdraw(1500, p6); //no overdraft

        }

        catch (AccountException e) { Console.WriteLine(e.Message); }

        try

        {

            Bank.GetAccount("CK-100018"); //account does not exist

        }

        catch (AccountException e) { Console.WriteLine(e.Message); }

        try

        {

            Bank.GetPerson("Trudeau"); //user does not exist

        }

        catch (AccountException e) { Console.WriteLine(e.Message); }

        //show all transactions

        Console.WriteLine("\n\nAll transactions");

        foreach (var transaction in Bank.GetAllTransactions())

            Console.WriteLine(transaction);

        foreach (var keyValuePair in Bank.ACCOUNTS)

        {

            Account account = keyValuePair.Value;

            Console.WriteLine("\nBefore PrepareMonthlyReport()");

            Console.WriteLine(account);

            Console.WriteLine("\nAfter PrepareMonthlyReport()");

            account.PrepareMonthlyReport(); //all transactions are cleared, balance changes

            Console.WriteLine(account);

        }

        Logger.ShowLoginEvents();

        Logger.ShowTransactionEvents();
    }
}
