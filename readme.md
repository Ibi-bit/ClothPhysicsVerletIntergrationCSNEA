

Prerequisites

Git (with submodule support)

.NET SDK 9.x

Verify:
`dotnet --version `
(should start with 9.)
though 8 should also work



Clone (recommended: pulls submodules in one go)

```
git clone --recurse-submodules https://github.com/Ibi-bit/ClothPhysicsVerletIntergrationCSNEA.git
cd ClothPhysicsVerletIntergrationCSNEA
```

If you already cloned without submodules

`git submodule update --init --recursive`

Make sure you’re on the right branch

The GitHub UI shows working-interactive-features as the active branch. 
```
git checkout working-interactive-features
git pull
git submodule update --init --recursive
```
Restore + build (.NET 9)

The main project folder appears to be PhysicsCSAlevlProject. 
```
dotnet restore PhysicsCSAlevlProject
dotnet build -c Release PhysicsCSAlevlProject
```
Run

```
dotnet run --project PhysicsCSAlevlProject -c Release
```
Common troubleshooting

Submodule folder empty / missing files: re-run

`git submodule update --init --recursive`

You switched branches and submodules look “wrong”:
```
git submodule sync --recursive
git submodule update --init --recursive
```
