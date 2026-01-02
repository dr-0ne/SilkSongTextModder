## Silksong Text Modder

A mod that allows editing, overriding, and creation of new text items of Silksong's internal data. This can be used to change dialogue, names, tutorial text, menu items, etc.

[Original Mod]()
[Source Code](https://github.com/KyleNeubarth/SilkSongTextModder)  
[Text Asset Decrypter](https://github.com/KyleNeubarth/SilkSongTextAssetDecrypter) (Simple program to help you view Silksong's ingame text)

### How do I Modify Text?

This mod reads text in files located in **"\Hollow Knight Silksong\Hollow Knight Silksong_Data\Mods\TextModder"**.  
These files have a precise format where you specify the localization language code, Sheet ID, Entry ID, and finally text value. All values are seperated by ">". I tried to use XML, JSON parsing but bepinex/harmony would crash during that for some reason. If enough people complain I will likely change this.


For example, a file with this line of text will override the main menu's quit button text (normally "Quit Game") with "Give Up". Note that this will only apply to the "EN" language code. You could put this line in **"\Hollow Knight Silksong\Hollow Knight Silksong_Data\Mods\TextModder\giveuptest.txt"** and it will apply the next time you start the game.  
`EN>MainMenu>MAIN_QUIT>Give Up`

In order to figure out which "sheet" and "entry" the text you want to modify lives, you must inspect the existing game's TextAssets. See further instructions below or check out the [DecryptedTextSheets](https://www.nexusmods.com/hollowknightsilksong/mods/381?tab=files) from the original.

It is also possible to add new sheets and entries which the vanilla game does not use. You may be able to use these for modded content, but I haven't been able to test any of that. Good luck!

### How to I make a mod using this?
Create the text file like you would do normally for this mod. Then create the mod, place the text file in the mod folder (same folder where your `manifest.json` file is located)  
And lastly add a file into the same folder and call it "`TextModder`". This file has to be exactly that. No lower case letters no *.txt*'s, just "`TextModder`".

### How do I extract existing text from the game for reference?

**TLDR:** I have extracted and pinned a snapshot of the current English text sheets to this mod's files. Otherwise you will need to extract the data yourself.

Internally, Silksong keeps localized text in large XML tables called "sheets". Sheets are arbitrary groupings of text that the developers thought was helpful for organization (EN_Song contains all the text for needolin NPC sing along music, for example). These sheets are TextAssets stored in the resources assetbundle (**"\Hollow Knight Silksong\Hollow Knight Silksong_Data\resource.assets"**). I used [AssetRipper](http://https//github.com/AssetRipper/AssetRipper) to decompile the AssetBundle and view the contents.

**Note:** If using AssetRipper remember to specify the Unity Version in it's settings. Silksong is using Unity "6000.0.50f1", which can be found by looking at the properties of the executable.

Sheets have some laughable code obfuscation function which they are run through. Via decompiling Silksong's "Assembly.CSharp.dll" with [Dnspy](http://https//github.com/dnSpy/dnSpy) I found the obfuscation function which is pasted below.
```csharp
Decryption Functionpublic static byte[] Decrypt(byte[] encryptedBytes)
{
  byte[] result;
  using (RijndaelManaged rijndaelManaged = new RijndaelManaged
{
 Key = _keyArray,
 Mode = CipherMode.ECB,
 Padding = PaddingMode.PKCS7
})
  {
result = rijndaelManaged.CreateDecryptor().TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
  }
  return result;
}
private static readonly byte[] _keyArray = Encoding.UTF8.GetBytes("UKu52ePUBwetZ9wNX88o54dnfKRu0T1l"); 
```