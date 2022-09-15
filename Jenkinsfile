pipeline {
    agent any
    
    stages {
        stage('build') {
            steps {
                dotnetRestore project: 'Blog.ActivityHub.sln'
                dotnetBuild configuration: 'Release', noRestore: true
            }
        }
        
        stage('test') {
            steps {
                dotnetTest configuration: 'Release', noBuild: true, logger: '"trx;LogFileName=test_results.trx"', collect: 'XPlat Code Coverage'
            }
            
            post {
                always {
                    xunit followSymlink: false, reduceLog: false, tools: [MSTest(excludesPattern: '', pattern: '**/test_results.trx', stopProcessingIfError: true)]
                    cobertura coberturaReportFile: '**/coverage.cobertura.xml'
                }
            }
        }
        
        stage('publish') {
            steps {
                dotnetPublish project: 'src/Blog.ActivityHub.Api/Blog.ActivityHub.Api.csproj', configuration: 'Release', noBuild: true, outputDirectory: 'publish/api'
                dotnetPublish project: 'src/Blog.ActivityHub.Web/Blog.ActivityHub.Web.csproj', configuration: 'Release', noBuild: true, outputDirectory: 'publish/web'
            }
            
            post {
                success {
                    archiveArtifacts artifacts: 'publish/**', followSymlinks: false
                }
            }
        }
        
        stage('deploy') {
            when { branch 'main' }
            environment {
                DOCKER_CREDS = credentials('aca9c2eb-1a97-4da0-9686-63bfdfd9be0d')
                CF_API_TOKEN = credentials('34681ec3-4bed-40a0-b6e4-be02b648cd67')
            }
            
            steps {
                sh label: 'authenticate docker repository', script: 'docker login -u $DOCKER_CREDS_USR -p $DOCKER_CREDS_PSW reg.mattcrook.io'
                sh label: 'containerize api', script: 'docker build -t reg.mattcrook.io/blog/activityhub:latest -t reg.mattcrook.io/blog/activityhub:$BUILD_ID -f src/Blog.ActivityHub.Api/Dockerfile .'
                sh label: 'push api container (build)', script: 'docker image push reg.mattcrook.io/blog/activityhub:$BUILD_ID'
                sh label: 'push api container (latest)', script: 'docker image push reg.mattcrook.io/blog/activityhub:latest'
                
                sh label: 'push web app', script: 'CLOUDFLARE_API_TOKEN=$CF_API_TOKEN wrangler pages publish publish/web/wwwroot --project-name matt-activityhub --branch main'
            }
            
            post {
                always {
                    sh label: 'logout docker repository', script: 'docker logout reg.mattcrook.io'
                }
            }
        }
    }
}