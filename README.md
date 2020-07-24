# F
C# support for decoupling Data, state and Logic
<br><br>
## Motivation

A desirable 'functional' programming paradign (as opposed to classic OOP) is one in which there is clear separation between Data, State and Logic:
- **Data** represents 'values'. Data is immutable (cannot change once created) and has value semantics (for equality etc). Data may contain methods returning it's values or their derivatives, and/or methods that return new mutations of itself. However its methods do not interact with States, Logic or external resources.
- **State** represents 'memory'. Its only functionality is access and mutation of its internal Data storage. A State does not mutate other States or use any Logic. A State can be publically available or only passed to Logic where/as needed. (Note that State here is different from state/stateful/stateless with a lowercase 's' which are commonly used to mean 'with a value' etc)
- **Logic** represents 'behaviour'. Is is pure functionality that links input (from UI etc), Data and State(s) and is the only entity that can mutate State(s).

An OOP program will have objects of type 'Employee ' that know their name and address and can 'ChangeAddress()', and a list etc of Employee objects storeing all instances. While this design maybe useful for some scenarios it has major limitations: Archiving/journaling/reasoning about state (Employee) changes is difficult, multithreading is difficult as every Employee instance can change its internal state, refactoring/reusing logic and data is difficult as they are coupled together with the state, etc<br>
A 'functional' paradigm will have an immutable 'EmployeeRecord' (Data) having name and address, a separate 'EmployeeArchive' State keeping the current dog records and a backlog (ie of address changes), and a 'HR' (Logic) that can ie fetch an Employee record from the Archive and change it's address.<br>
This is a vast topic but in short separating Data, State and Logic will give you programming superpowers.  A good summary of the bebefit of a such a 'functional' approach vs OOP can be found [here](https://clojure.org/about/state).

C# was developed as an OOP language where data state and logic are strongly coupled in classes. This makes coding in such a 'functional' paradigm challenging:
- Creating immutable data with value semantics is challenging as C# Objects are by default mutable (though the addition of read-only properties is a good step) and correctly implementing value semantics is not trivial . Immutable containers were recently added to .NET but they are cumbersome to use and have reference semantics. 
- Encapsulating a state with its access/mutation API is challenging though recent language additions can give good solutions.
- Stateless logic can be expressed by static classes and static functions

The purpose of the F package is to greatly simplify the creation of data (immutable objects with value semantics), and to provide a mechanism for creating, accessing and mutating states.
<br><br>
## Components

**[FData](https://github.com/kofifus/F/wiki/FData)**

Deriving `FData` declares an object as Data - immutable with value semantics. Some core types (ie strings, Tuples etc) are FData even without directly deriving from the FData base.<br>
An `FData` type/object my contain non-public mutable members as long as it is publically immutable.


**FRecord** 

Allow easy creation of `FData` types (records).

**F collections (FList, FSet, FDict, FQueue, FArray)**

`FData` versions of commonn containers with enhanced API.

**FState**

Encapsulate an `FData` object so that the _only_ way to modify it is through clearly defined access/mutation mechanisms. Two concrete implementations are provided - `FLockedState` which provides thread safety by locking on mutation, and `FJournaledLockedState` which also archive previous versions of the State.

**FWrapper**

Allow the easy creation of a new (`FData`) type which encapsulates another (`FData`) type. 

**FComposer**

Allow the easy creation of a new (`FData`) type which encapsulates another type which is not (`FData`) itself.  
<br>
## Example

**Data:**
```
public class Employee : FRecord<Employee> {
  public string Name { get; }
  public readonly int Age;
  public FSet<string> Phones { get; }
  
  public Employee(string name, int age, FSet<string> phones) => (this.Name, this.Age, this.Phones) = (name, age, phones);
}
```

Notes:
- `FSet` is an immutable hashset with value semantics and other additions.<br>
- Deriving from `FRecord` makes `Employee` an `FData` (immutable with value semantics), that is it gets `Equals`/`==`/`!=` and `GetHashCode` that uses all it's members. These are generated using reflection and cached in delegates for efficiency. This means that `Employee` can be ie stored in an `FSet` or be itself a key in an `FDict`.<br>
- Deriving `FRecord` also gives `Employee` a `With` method that allows easy creation of mutations (ie `emp2 = emp1.With(x => x.Name, "newname");`). `With` expressions are resolved using resolution and cached in delegates for efficiency.<br>
- `FRecord` will verify (in DEBUG mode) that all of `Employee`'s public fields/properties are publically readonly and `FData` themselves.<br>
<br>

**State:**
```
public static class Store {
  public static readonly FLockedState<FDict<string, Employee>> Employees = FLockedState.Create(new FDict<string, Employee>());
}
```

Notes:
- Store holds the State of the program in this case. It is implemented as a static class with readonly `FState` fields.
- `FLockedState` is a mutable State that locks itself before allowing mutation so that the _only_ way to change it is threadsafe. It has three methods: `Ref` locks and mutate, `In` locks and allows readonly access, and `Val` allows threadsafe readonly access of a possibly stale value.
Using `Ref` and `In` hides locking and eliminate multithreading issues where locking was forgotten. Using 'Val' whereever stale values can be tolerated prevents unecessary locking while preserving thready safety.
- `VDict` is an immutable dictionary with value semantics and other additions. 
<br>

**Logic:**
```
public static class EmployeeLogic {
  public static void AddEmployee(Employee employee) {
    Store.Employees.Ref((ref FDict<string, Employee> storeEmployees) => {
      storeEmployees += (employee.Name, employee);
    });
  }

  public static bool AddEmployeePhone(string name, string phone) {
    return Store.Employees.Ref((ref FDict<string, Employee> storeEmployees) => {
      var (ok, newStoreEmployees) = storeEmployees.With(name, x => x.Phones, phones => phones + phone);
      if (ok) storeEmployees = newStoreEmployees; // change the State
      return ok;
    });
  }

  public static (bool, Employee) GetEmployee(string name) => Store.Employees.Val[name];

  public static IEnumerable<string> GetEmployeePhones(string name) {
    var (ok, employee) = Store.Employees.Val[name];
    return ok ? employee.Phones : Enumerable.Empty<string>();
  }
}
```

Notes:
- EmployeeLogic is Logic - a collection of static (pure) methods.<br>
- In this sample the `Employees` state is part of a global `Store` this is common and convinient. Another way would be passing the `Store.Employees` State to each `EmployeeLogic` method.
- In this sample we have a dedicated Logic for a single `Data` (`Employee`) however this is not mandatory and grouping of Logic methods is a design decision.
- `AddEmployee` uses `Ref` to acquire a reference access to mutate the employees dictionary State and add/set an employee.
Using `Ref` is the _only_ way to change `Store.Employee` and becasue it is an `FLockedState` this operation is threadsafe (a lock is acquired internally).
- Note the use of `+=` to add a (key, value) to the dictionary. `F` collections prefer operator overloading for adding/removing  (in the same way that basic `string` does) as they are more suiltable for immutable types.
- `AddEmployeePhone` similary uses a `Ref` to mutate `Store.Employees` in a threadsafe way. It uses `With` to calculate and return a mutation of storeEmployees with a mutated Phones property, and assign it to back to the State.
- Note the way success is returned in `ok`. Using C# Nullable reference types (`#nullable enable`), gives a compiler warning if you try to access `storeEmployees` without checking that `ok` is true. `F` uses this pattern for all collections boundary checks and does not throw exceptions in those cases. 
- `GetEmployee` and `GetEmployeePhones` uses `Val` to get access to the current value of `Store.Employees`. No lock is taken in this case so the result may be stale which is fine in this case. However the call is still threadsafe as the returned value (being an `FData`) is immutable. This kind of threadsafe access to possibly stale values wherever possible adds great effiency.
<br>

**Main:**

```
public static void Main() {
  var dave = new Employee("Dave", 30, new FSet<string>("123"));

  var john = dave.With(x => x.Name, "John");
  Console.WriteLine("dave RefrenceEquals john ? " + (Object.ReferenceEquals(dave, john) ? "true" : "false")); // false
  Console.WriteLine("dave==john ? " + (dave==john ? "true" : "false")); // false

  var john1 = dave.With(x => x.Name, "John");
  Console.WriteLine("john RefrenceEquals john1 ? " + (Object.ReferenceEquals(john, john1) ? "true" : "false")); // false
  Console.WriteLine("john==john1 ? " + (john == john1 ? "true" : "false")); // true
  
  EmployeeLogic.AddEmployee(john);
  var (ok, storeJohn) = EmployeeLogic.GetEmployee("John");
  if (ok) {
    Console.WriteLine("john RefrenceEquals storeJohn ? " + (Object.ReferenceEquals(john, storeJohn) ? "true" : "false")); // true
    Console.WriteLine("john==storeJohn ? " + (john == storeJohn ? "true" : "false")); // true
  }
  
  EmployeeLogic.AddEmployeePhone(john.Name, "456");
  var (ok1, storeJohn1) = EmployeeLogic.GetEmployee("John");
  if (ok1) {
    Console.WriteLine("storeJohn RefrenceEquals storeJohn1 ? " + (Object.ReferenceEquals(storeJohn, storeJohn1) ? "true" : "false")); // false
    Console.WriteLine("storeJohn==storeJohn1 ? " + (storeJohn == storeJohn1 ? "true" : "false")); // false
  }
  
  var storeJohnPhones = EmployeeLogic.GetEmployeePhones(john.Name);
  Console.WriteLine(string.Join(",", storeJohnPhones)); // 123, 456
}
```
Notes:
- Main is also Logic.
- Main is threadsafe as all objects are immutable. Locking is only used where a State is mutated.

