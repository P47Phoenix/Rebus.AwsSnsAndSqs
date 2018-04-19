
node {
    def gitHash = ""
    stage('Clean Workspace') {
        deleteDir()
    }
    stage('Getting Latest') { // for display purposes
       //Get some code from a GitHub repository
        git url:'git@ghe.coxautoinc.com:Mike-Connelly/Rebus.AwsSnsAndSqs.git', branch:"${env.BRANCH_NAME}"
        def hashsplit = bat ( returnStdout:true, script:"git rev-parse --verify HEAD").split("\n?\r")
        gitHash = hashsplit[2].trim()
    }
    stage('Restore nuget packages')
    {
        bat "tools\\nuget.exe restore ./Rebus.AwsSnsAndSqs.sln"
    }
    stage('Build')
    {
        bat "${env.MSBUILD2017Exe} ./Rebus.AwsSnsAndSqs.sln /p:Configuration=Release"
    }
    stage('test')
    {
        bat "tools\\nuget.exe install OpenCover -Version 4.6.519"
        bat "tools\\nuget.exe install OpenCoverToCoberturaConverter -Version 0.3.1"
        bat "tools\\nuget.exe install NUnit.Console -Version 3.8.0"
        bat ".\\OpenCover.4.6.519\\tools\\OpenCover.Console.exe -target:\"NUnit.ConsoleRunner.3.8.0\\tools\\nunit3-console.exe\" -targetargs:\"Rebus.AwsSnsAndSqsTests\\bin\\Release\\net45\\Rebus.AwsSnsAndSqsTests.dll\" -register:user -filter:\"+[Rebus.AwsSnsAndSqs]*\""
        step([$class: 'NUnitPublisher', testResultsPattern: 'TestResult.xml', debug: false, keepJUnitReports: true, skipJUnitArchiver:false, failIfNoResults: true])
        step([$class: 'CoberturaPublisher', coberturaReportFile: 'outputCobertura.xml'])
    }
    stage('Pack')
    {
        env.AssemblyVersion = PowerShell('.\\GetAssemblyVersion.ps1')

        def isAlpha = true
        if(env.BRANCH_NAME.equals('master'))
        {
            isAlpha = false
        }

        if(isAlpha)
        {
            env.AssemblyVersion = env.AssemblyVersion + '-alpha'
        }

        echo "package version ${env.AssemblyVersion}"

        bat "${env.NuGetExe} pack ${env.Workspace}\\Rebus.AwsSnsAndSqs\\Rebus.AwsSnsAndSqs.csproj -Version ${env.AssemblyVersion} -Properties Configuration=Release -OutputDirectory ${env.Workspace} -Symbols"

        bat "${env.NuGetExe} push ${env.Workspace}\\Rebus.AwsSnsAndSqs.*.nupkg -Source ${env.VinNuGetServer} -ApiKey ${env.VinNuGetApiKey}"
    }
}

def PowerShell(psCmd) {
    echo psCmd
    def script = "powershell.exe -NonInteractive -ExecutionPolicy Bypass -Command \"\$ErrorActionPreference='Stop';& $psCmd;EXIT \$global:LastExitCode\""
    def result = bat (returnStdout: true, script: script).split("\n?\r")

    return result[2].trim()
}