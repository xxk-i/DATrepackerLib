# DATrepackerLib
C# Library for repacking Nier:Automata DAT files

Built with .NET Framework 4.6

Give FilePackInfo a path and pass it to Writer to generate the file.

Example Usage (Wpf):

```        
private void Browse_Click(object sender, RoutedEventArgs e)
{
  fileBrowserDialog.IsFolderPicker = true;
  CommonFileDialogResult result = fileBrowserDialog.ShowDialog();
  
  if (result == CommonFileDialogResult.Ok)
  {
  
  nDirectory = fileBrowserDialog.FileName;
  selectedTextPath.Text += "\n" + InDirectory;
  }
  
}

private void Generate_DAT(object sender, RoutedEventArgs e)
{
  FilePackInfo packInfo = new FilePackInfo(InDirectory);
  Writer writer = new Writer(packInfo);
  writer.WriteToFile(InDirectory + "\\dat.dat");
}
```
