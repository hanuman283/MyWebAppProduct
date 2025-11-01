pipeline {
    agent any
    
    environment {
        GCP_PROJECT_ID = credentials('gcp-project-id')
        IMAGE_NAME = "mywebappproduct"
        IMAGE_TAG = "${BUILD_NUMBER}"
    }
    
    stages {
        stage('Build') {
            steps {
                // First build the .NET application
                sh 'dotnet restore'
                sh 'dotnet build --configuration Release'
                sh 'dotnet publish -c Release -o ./publish'
                
                // Then build Docker image
                script {
                    def imageUrl = "gcr.io/${GCP_PROJECT_ID}/${IMAGE_NAME}:${IMAGE_TAG}"
                    sh "docker build -t ${imageUrl} ."
                }
            }
        }
        
        stage('Push') {
            steps {
                withCredentials([file(credentialsId: 'gcp-service-account-key', variable: 'GCP_KEY')]) {
                    sh '''
                        set -e  # Exit on any error
                        
                        # Activate service account
                        if ! gcloud auth activate-service-account --key-file=$GCP_KEY; then
                            echo "Failed to activate service account"
                            exit 1
                        fi
                        
                        # Check and create repository if it doesn't exist
                        echo "Checking GCR repository..."
                        LOCATION="us" # adjust this to your desired location
                        REPO_NAME="${IMAGE_NAME}"
                        
                        if ! gcloud artifacts repositories describe ${REPO_NAME} \
                            --project=${GCP_PROJECT_ID} \
                            --location=${LOCATION} > /dev/null 2>&1; then
                            echo "Creating GCR repository..."
                            gcloud artifacts repositories create ${REPO_NAME} \
                                --project=${GCP_PROJECT_ID} \
                                --repository-format=docker \
                                --location=${LOCATION} \
                                --description="Docker repository for ${IMAGE_NAME}"
                        fi
                        
                        # Ensure .docker directory exists
                        echo "Creating .docker directory..."
                        if ! mkdir -p ${WORKSPACE}/.docker; then
                            echo "Failed to create .docker directory"
                            exit 1
                        fi
                        
                        # Configure docker only for gcr.io (main region)
                        echo "Configuring Docker credential helper..."
                        if ! echo '{"credHelpers":{"gcr.io":"gcloud"}}' > ${WORKSPACE}/.docker/config.json; then
                            echo "Failed to write Docker config"
                            exit 1
                        fi
                        
                        # Push the image
                        echo "Pushing image to GCR..."
                        if ! docker push gcr.io/${GCP_PROJECT_ID}/${IMAGE_NAME}:${IMAGE_TAG}; then
                            echo "Failed to push image"
                            exit 1
                        fi
                        
                        # Clean up docker config (keep the directory for future runs)
                        echo "Cleaning up..."
                        rm -f ${WORKSPACE}/.docker/config.json || true
                    '''
                }
            }
        }
        
        stage('Deploy') {
            steps {
                withCredentials([file(credentialsId: 'gcp-service-account-key', variable: 'GCP_KEY')]) {
                    sh '''
                        gcloud auth activate-service-account --key-file=$GCP_KEY
                        gcloud run deploy ${IMAGE_NAME} \
                            --image gcr.io/${GCP_PROJECT_ID}/${IMAGE_NAME}:${IMAGE_TAG} \
                            --region us-central1 \
                            --platform managed \
                            --allow-unauthenticated
                    '''
                }
            }
        }
    }
    
    post {
        success {
            echo 'Deployment successful! Service URL:'
            sh '''
                gcloud run services describe ${IMAGE_NAME} \
                    --region us-central1 \
                    --format="value(status.url)"
            '''
        }
    }
}