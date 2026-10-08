variable "DOCKER_REGISTRY" {
  default = "docker.io"
}

variable "DOCKER_NAMESPACE" {
  default = "criticalmanufacturing"
}

variable "DOCKER_TAG" {
  default = "latest"
}

variable "DOCKER_PLATFORM" {
  default = "linux/amd64"
}

group "default" {
  targets = ["sos", "sos-ubi"]
}

target "sos" {
  context    = "src/sidecar"
  dockerfile = "Dockerfile"
  tags       = ["${DOCKER_REGISTRY}/${DOCKER_NAMESPACE}/sos:${DOCKER_TAG}"]
  platforms  = [DOCKER_PLATFORM]
}

target "sos-ubi" {
  context    = "src/sidecar/ubi"
  dockerfile = "Dockerfile"
  tags       = ["${DOCKER_REGISTRY}/${DOCKER_NAMESPACE}/sos-ubi:${DOCKER_TAG}"]
  platforms  = [DOCKER_PLATFORM]
}
