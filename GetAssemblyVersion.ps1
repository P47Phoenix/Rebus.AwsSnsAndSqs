$path = $ENV:WORKSPACE + "\Rebus.AwsSnsAndSqs\bin\Release\net45\Rebus.AwsSnsAndSqs.dll"
$assembly = [Reflection.Assembly]::LoadFile($path)
$assemblyName = $assembly.GetName()
$assemblyName.Version.ToString()