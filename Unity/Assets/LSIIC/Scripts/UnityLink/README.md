This folder is the same folder as in `LSIIC\Assembly-CSharp.LSIIC.mm\UnityLink`.

It's been created with a symbolic link, which you can do in Windows by:

```
mklink /D /H <path to H3VRMods\Assets\LSIIC\Scripts\UnityLink> <path to LSIIC\Assembly-CSharp.LSIIC.mm\UnityLink>
```

And on Linux and macOS by:
```
ln -s <path to LSIIC\Assembly-CSharp.LSIIC.mm\UnityLink> <path to H3VRMods\Assets\LSIIC\Scripts\UnityLink> 
```