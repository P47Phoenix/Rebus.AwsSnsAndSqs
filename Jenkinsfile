
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
        // nuget versioning is controlled here
        env.AssemblyVersion = "4.0.${env.BUILD_NUMBER}"

        def isAlpha = true

        if(env.BRANCH_NAME.equals('master'))
        {
            isAlpha = false
        }

        if(isAlpha)
        {
            env.AssemblyVersion = env.AssemblyVersion + '-alpha-' + env.BRANCH_NAME
        }
        echo "package version ${env.AssemblyVersion}"        
        bat "Tools\\Aversion\\Aversion.exe patch -ver \"${env.AssemblyVersion}\" -in Rebus.AwsSnsAndSqs\\Properties\\AssemblyInfo.cs -out Rebus.AwsSnsAndSqs\\Properties\\AssemblyInfo_Patch.cs -token \"4.0.0.0\""
        bat "del /Q Rebus.AwsSnsAndSqs\\Properties\\AssemblyInfo.cs"
        bat "${env.MSBUILDExe} ./Rebus.AwsSnsAndSqs.sln /p:Configuration=Release /p:PackageVersion=\"${env.AssemblyVersion}\""
    }
    stage('test')
    {
        bat ".\\tools\\OpenCover.4.6.519\\tools\\OpenCover.Console.exe -register:Path32 -target:\"tools\\NUnit.ConsoleRunner.3.8.0\\tools\\nunit3-console.exe\" -targetargs:\"Rebus.AwsSnsAndSqsTests\\bin\\Release\\net45\\Rebus.AwsSnsAndSqsTests.dll\" -filter:\"+[Rebus.AwsSnsAndSqs]*\""
        step([$class: 'NUnitPublisher', testResultsPattern: 'TestResult.xml', debug: false, keepJUnitReports: true, skipJUnitArchiver:false, failIfNoResults: true])
        def opencoverExists = fileExists 'results.xml'
        if(opencoverExists)
        {
            bat 'Tools\\OpenCoverToCoberturaConverter.0.3.1\\tools\\OpenCoverToCoberturaConverter.exe "-input:results.xml" "-output:Cobertura.xml"'
            step([$class: 'CoberturaPublisher', coberturaReportFile: 'outputCobertura.xml'])
        }
        else
        {
            echo "open cover could not run properly"
        }
    }
    stage('publish')
    {
        
    }
}

def PowerShell(psCmd) {
    echo psCmd
    def script = "powershell.exe -NonInteractive -ExecutionPolicy Bypass -Command \"\$ErrorActionPreference='Stop';& $psCmd;EXIT \$global:LastExitCode\""
    def result = bat (returnStdout: true, script: script).split("\n?\r")

    return result[2].trim()
}