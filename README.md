# F
C# support for decoupling Data, state and Logic
<br><br>
## Motivation

A desirable 'functional' programming paradign (as opposed to classic OOP) is one in which there is clear separation between Data, State and Logic:
- **[Data](https://github.com/kofifus/F/wiki/Data)** represents 'values'. Data is immutable (cannot change once created) and has value semantics (for equality etc). Data may contain methods returning it's values or their derivatives, and/or methods that return new mutations of itself. However its methods do not interact with States, Logic or external resources.
- **[State](https://github.com/kofifus/F/wiki/State)** represents 'memory'. Its only functionality is access and mutation of its internal Data storage. A State does not mutate other States or use any Logic. A State can be publically available or only passed to Logic where/as needed. (Note that State here is different from state/stateful/stateless with a lowercase 's' which are commonly used to mean 'with a value' etc)
- **Logic** represents 'behaviour'. Is is pure functionality that links input (from UI etc), Data and State(s) and is the only entity that can mutate State(s).

An OOP program will have objects of type 'Employee' that know their name and address and can 'ChangeAddress()', and an collection of Employee objects storing all instances. While this design maybe useful for some scenarios it has major limitations: Archiving/journaling/reasoning about Employee changes is difficult as there is no control on where/when such changes happen, multithreading is difficult as every Employee instance can change its internal state, refactoring/reusing logic and data is difficult as they are coupled together with the state.<br>
A 'functional' paradigm will have an immutable 'EmployeeRecord' (Data) having name and address, a separate 'EmployeeArchive' State with clear access/mutation/archiving mechanisms, and an 'HR' Logic that can ie fetch an Employee record from the Archive and change it's address etc.<br>
This is a vast topic but in short separating Data, State and Logic will give you programming superpowers.  A good summary of the bebefit of a such a 'functional' approach vs OOP can be found [here](https://clojure.org/about/state).

C# was developed as an OOP language where data state and logic are strongly coupled in classes. This makes coding in such a 'functional' paradigm challenging:
- Creating immutable data with value semantics is challenging. C# immutable containers are cumbersome to use and have reference semantics. 
- Encapsulating a state with its access/mutation API is challenging.
- Stateless logic can be expressed by static classes and functions

The purpose of the F package is to greatly simplify the creation of Data, and to provide a mechanism for creating, accessing and mutating States without sacrificing efficency.
<br><br>
## Components

**[Data](https://github.com/kofifus/F/wiki/Data)**

Data in `F` is represented by classes implementing `IEquatable<T>` (ie records), where all fields are read only and are Data themselves. Some core types (ie strings, Tuples etc) are considered Data as well.<br>

A debug runtime verifier is provided to assert all types in a solution are Data allowing user defined exceptions.

F also contains Data (immutable with value semantics) versions of commonn [collections](https://github.com/kofifus/F/wiki/Collections) (Seq, Set, Map, Que, Arr) with enhanced API.

**[State](https://github.com/kofifus/F/wiki/State)**

Stores a Data object so that the _only_ way to access/mutate it is through clearly defined mechanisms. Two concrete implementations are provided - `LockedState` which provides thread safety by locking on mutation, and `JournaledLockedState` which also archive previous versions of the State.

## Usage

While it will definietly work to mix in elements of `F` into a project, the recommended useage is to structure your entire code to decouple Data State and Logic and using `F` throughout. In this case your code should have _no_ non-static classes at all - all Data is in `record`s, all State and Logic are in static classes.<br>
A debug mode runtime verifier/helper is included (`Data.AssertF()`) that will try to throw exceptions where `F` directives are not followed (ie mutable record members).<br> 
Inevitably in many cases, some .NET types that are not Data have to be used. In some of these cases it is possible to encapsulate these types inside a `Data` type and move their mutable part to a `State` (an example is given of encapsulating `JToken` in this way). Other types cannot be converted (ie classes inheriting `EntityFrameworkCore.DbContext` which is not immutable) and have to be managed carefuly. 
<br><br>
## Example

**Data:**
```
record Employee(string Name, int Age, Set<string> Phones);
```

Notes:
- `Set` is a Data version of List, one of the F [collections](https://github.com/kofifus/F/wiki/Collections). This means `Employee` is Data as well and so can be ie stored in a `Set` or be itself a key in an `Map`.<br>
<br>

**State:**
```
record Store {
  public readonly LockedState<Map<string, Employee>> Employees = new(new());
}

static readonly Store S = new();
```

Notes:
- In this sample the `Employees` State is part of a global `Store` which is convinient. Another way would be to pass an `Employees` State to differen Logic methods where/as nedded.
- `LockedState` locks itself before allowing mutation so that the _only_ way to change it is threadsafe. It has three methods: `Ref` - locks and mutate, `In` - locks and allows readonly access, and `Val` - allow threadsafe readonly access of a possibly stale value.
Using `Ref` and `In` hides locking and eliminate multithreading issues where locking was forgotten. Using 'Val' whereever stale values can be tolerated prevents unecessary locking while preserving thready safety.
- `Map` is an immutable dictionary with value semantics and other additions. 
<br>

**Logic:**
```
static class EmployeeLogic
  public static void AddEmployee(Employee employee) {
    S.Employees.Ref((ref Map<string, Employee> storeEmployees) => {
      storeEmployees += (employee.Name, employee);
    });
  }

  public static bool AddEmployeePhone(string name, string phone) {
    return S.Employees.Ref((ref Map<string, Employee> storeEmployees) => {
      var employee = storeEmployees[name];
      if (employee is null) return false;
      var mutatedEmployee = employee with { Phones = employee.Phones + phone };
      storeEmployees += (name, mutatedEmployee);
      return true;
    });
  }

  public static Employee? GetEmployee(string name) => S.Employees.Val[name];

  public static IEnumerable<string> GetEmployeePhones(string name) {
    var employee = GetEmployee(name);
    return employee is object ? employee.Phones : Enumerable.Empty<string>();
  }
}
```

Notes:
- EmployeeLogic is Logic - a collection of static (pure) methods.<br>
- In this simple sample we have a dedicated Logic for a single `State` (`Employees`) however this is not mandatory and the grouping of Logic methods is a design decision.
- `AddEmployee` uses `Ref` to acquire a reference access to mutate the employees State and add/set an employee.
Using `Ref` is the _only_ way to change `Store.Employee` and becasue it is a `LockedState` this operation is threadsafe (a lock is acquired internally).
- Note the use of `+=` to add a (key, value) to the Map (dictionary). `F` collections prefer operator overloading for adding/removing  (in the same way that basic `string` does) as they are more suitable and convineient for immutable types.
- `AddEmployeePhone` similary uses a `Ref` to mutate `Employees` in a threadsafe way. It uses `with` to calculate and return a mutation of storeEmployees with a mutated Phones property, and assign it to back to the State. 
- `GetEmployee` and `GetEmployeePhones` uses `Val` to get access to the current value of `Employees`. No lock is taken in this case so the result may be stale which is fine in this case. However the call is still threadsafe as the returned value (being an `FData`) is immutable. This kind of threadsafe access to possibly stale values wherever possible adds great effiency.
<br>

**Main:**

```
public static void Main() {
  static void Log(string s) { Console.WriteLine(s); }
  Data.AssertF(); // optional, check for F compliance
  
  var dave = new Employee("Dave", 30, new Set<string>("123"));
  var john = dave with { Name = "John" };

  EmployeeLogic.AddEmployee(john);
  var storeJohn = EmployeeLogic.GetEmployee("John");
  if (storeJohn is null) return;
  Log("john RefrenceEquals storeJohn ? " + (Object.ReferenceEquals(john, storeJohn) ? "true" : "false")); // true
  Log("john==storeJohn ? " + (john == storeJohn ? "true" : "false")); // true

  EmployeeLogic.AddEmployeePhone(john.Name, "456");
  var storeJohn1 = EmployeeLogic.GetEmployee("John");
  if (storeJohn1 is null) return;
  Log("storeJohn RefrenceEquals storeJohn1 ? " + (Object.ReferenceEquals(storeJohn, storeJohn1) ? "true" : "false")); // false
  Log("storeJohn==storeJohn1 ? " + (storeJohn == storeJohn1 ? "true" : "false")); // false

  var storeJohnPhones = EmployeeLogic.GetEmployeePhones(john.Name);
  Log(string.Join(",", storeJohnPhones)); // 123, 456
}
```
Notes:
- Main is also Logic.
- Main is threadsafe as all objects are immutable. Locking is only used where a State is mutated.
- `Data.AssertF()` does nothing in Release mode. In Debug mode it will reflect over all the classes in the assembly and throw exceptions where it detects deviations from F standards. This is useful to enusre the entire project uses a 'functional' paradigm


