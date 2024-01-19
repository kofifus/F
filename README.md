# F
C# support for decoupling Data, State and Logic
<br><br>

C# was developed as an OOP language where data, state and logic are strongly coupled in instances   
of mutable classes. This makes coding in a 'functional' paradigm difficult:

- Encapsulating a State and passing it around with a dedicated access/mutation API (e.g, locking for   
  thread safety) is challenging.  

- Creating immutable Data with value semantics is challenging and enforcing immutability across a   
  solution is challenging. Also Immutable containers are cumbersome to use and have reference semantics. 

- Encapsulating pure Logic is challenging. Using static classes for logic is cumbersome and requires    
  passing relevant states to each function. 

<br />


Newer language features (records, refs, lambdas etc) allow better functional programming in C#.   
**F** Introduces a clear separation between State, Data and Logic along with support mechanisms:<br/>

- [Data](https://github.com/kofifus/F/wiki/Data) 

  Data are immutable types with value semantics (including for == and !=) implemented as C# records.  
  F also provides Data versions of .NET collections with enhanced API, see [Collections](https://github.com/kofifus/F/wiki/Collections): 
     
  ```
  record EmployeeData(string Name, Set<string> Phones);
  ```
  `Set` is the F version of `ImmutableHashSet` and is itself Data and so can be ie stored in a `Set` or be itself a key in a `Map` (dictionary).  
  <br/>

  ```
  record PhonesData : SetBase<PhonesData, string> {
    public PhonesData() : base() { }
    public PhonesData(params string[] phones) : base(phones) { }
  }

  record EmployeeData(string Name, PhonesData Phones);

  var employeeList = new Lst<EmployeeData>(); 
  ```
  `SetBase` is the base class for `Set`, Inheriting `SetBase` (rather than using `Set` directly) makes `PhonesData` a  
  separate type to encourage type safety.  

  `Lst` is the F Data version of `ImmutableList`.  
  <br/>    

  ```
  record EmployeesMapData : MapBase<EmployeesMapData, string, EmployeeData>;

  var daveData = new EmployeeData("Dave", new PhonesData("65321457"));
  var repository = new EmployeesMapData(("dave", daveData));
  
  var newDaveData = daveData with { Phones = daveData.Phones + "78901234" }; 
  repository += ("dave", newDaveData);

  var dave = repository["dave"];
  if (dave is not null) ...
  ```
  `MapBase` is the base class for `Map` (the F version of `ImmutableDictionary`)  
    
  Note the use of `+` to add to `Phones` and `+=` to add to `repository` . `F` collections prefer operator overloading  
  for adding/removing as they are more suitable and convenient for immutable types.  
  For `Map` '+' will overwrite existing values.

  Also note the index operator returning `T?` is preferred over `TryGetValue` for non-nullable reference 
  types as it is more natural.  
  `TryGetValue` is still available for nullable reference types or value types.
<br/>

- [State](https://github.com/kofifus/F/wiki/State) - an instance of a class implementing `IState` or `IReadOnlyState` to provide an explicit  
  &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;clear API  for creating, accessing and mutating the state:
  ```
  var johnData = new EmployeeData("John", new());
  var johnState = new LockedState<EmployeeData>(johnData); 
  
  IReadOnlyState<EmployeeData> JohnStateRO = johnState.ToIReadOnlyState;
  EmployeeData johnData = JohnStateRO.Val(); 

  IState<EmployeeData> JohnStateRW = johnState.ToIState;
  JohnStateRW.Val((ref EmployeeData johnData) => {
    johnData = johnData with { Phones = johnData.Phones + "78901234" };
  }); 
  ```
  Using a `LockedState` makes access and mutation of `johnState` thread safe (by acquiring a lock).

  `JohnStateRO` is a thread safe read-only access to the State that can be passed around.  
  `.Val()` temporarily locks the state and returns it's current (immutable) value.  
  <br>
  `JohnStateRW` is a thread safe read/write access to the State that can be passed around.  
  `.Val((red ...)` locks the state to allow mutation. Importantly it is the _only_ way to mutate the Data in a `johnState `.  
  <br>
  `.ToIReadOnlyState` and `.ToIState` are recommended but optional and serve to make the intention (read-only vs read/write) explicit. 
<br/>

- [Logic](https://github.com/kofifus/F/wiki/Logic) - a C# `class` that is initialized with access to specific states, this saves passing the state to each API call  
  &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;and provides precise access control: 

  ```
  class EmployeesLogic {
    readonly IState<EmployeesMapData> EmployeesMapState;
    readonly IReadOnlyState<ConfigData> ConfigState;

    public EmployeesModule(IStateRef<EmployeesMapData> employeesMapState, IStateVal<ConfigData> configState) {
      EmployeesMapState = employeesMapState;
      ConfigState) = configState;
    }

    public EmployeesMapData Val() => EmployeesMapState.Val();

    public PhonesData? GetEmployeePhones(string name) => EmployeesMapState.Val()[name]?.Phones;

    public bool AddEmployeePhone(string name, string phone) {
      return EmployeesMapState.Val((ref EmployeesMapData employeesMap) => {
        var employee = employeesMap[name];
        if (employee is null) return false;
        var newPhone = ConfigState.Val().PhoneCountryPrefix + phone;
        var mutatedEmployee = employee with { Phones = employee.Phones + newPhone }; 
        employeesMap+= (name, mutatedEmployee);
        return true;
      });
    }
  }
  ```
  `EmployeesLogic` is a Logic class - it's only (private & readonly) fields are the `States` that its  
  methods can access/mutate. In this case it has a mutation access to `EmployeesMapState` and a  
  read only access to `ConfigState`.  
  <br/>
  `EmployeesLogic` provides public read-only access to a private state via the `Val()` method,  
  this is a common (but optional) design decision.  
  <br/>
  `GetEmployeePhones` uses `Val()` to get access to the current value of `Employees`.  
  <br/>
  `AddEmployeePhone` uses a `Val((ref ...)` to mutate `EmployeesMapState`. `Val` here also returns a value  
  to the surrounding scope.  
  Using `Val((ref ...)` is the _only_ way to change `EmployeesMapState` and because it is a `LockedState`  
  this operation is thread-safe (a lock is acquired internally).  
  <br/>
  <br/>
- [Validator](https://github.com/kofifus/F/wiki/Validator) Debug time verifier that checks all types in the assembly adhere to the State/Data/Logic separation:

  ```
  record BadData(string Name, HashSet<int> Phones);

  Validator.Run();  
  // throws exception: Data record BadData member Phones cannot be a class
  ```

  The Validator though optional is in many ways the heart of `F`. While it will definitely work to mix in elements 
of `F`  
into a project, ideally the entire code base is structured to decouple Data State and Logic using `F` throughout  
and calling `Validator.Run()` for validating.<br><br>
Inevitably in many cases, some .NET types that are not `Data` have to be used. In some of these cases it  
is possible to encapsulate these types inside a `Data` type and move their mutable part to a `State`.  
Other types cannot be converted (ie classes inheriting `EntityFrameworkCore.DbContext` which is not   
immutable) and have to be managed carefully using `[FIgnore]`. 
<br><br>
## Installation

Add the `.cs` files to your project

In your sources add `using F;`
<br><br>
## License

[Apache 2.0](https://www.apache.org/licenses/LICENSE-2.0)




