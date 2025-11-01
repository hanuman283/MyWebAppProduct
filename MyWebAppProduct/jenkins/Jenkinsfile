pipeline {
  agent any
  environment {
    IMAGE = "gcr.io/${env.GCP_PROJECT_ID}/mywebappproduct:${env.BUILD_NUMBER}"
  }
  stages {
    stage('Build') {
      steps {
        sh 'docker build -t $IMAGE .'
      }
    }
    stage('Push') {
      steps {
        withCredentials([file(credentialsId: 'gcp-service-account-key', variable: 'GCP_KEY')]) {
          sh 'gcloud auth activate-service-account --key-file=$GCP_KEY'
          sh 'gcloud auth configure-docker'
          sh 'docker push $IMAGE'
        }
      }
    }
    stage('Deploy') {
      steps {
        withCredentials([file(credentialsId: 'gcp-service-account-key', variable: 'GCP_KEY')]) {
          sh 'gcloud auth activate-service-account --key-file=$GCP_KEY'
          sh 'gcloud run deploy mywebappproduct --image=$IMAGE --region us-central1 --platform managed --allow-unauthenticated'
        }
      }
    }
  }
}
