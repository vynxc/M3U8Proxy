# M3U8 Proxy Docker Image

This Docker image contains an M3U8 proxy that can stream HLS files. It is built on the .NET platform and can be easily deployed using Docker.

## Prerequisites

Before you begin, make sure you have the following installed:

- Docker: [Install Docker](https://docs.docker.com/get-docker/)
- .NET SDK 7.0: [Install .NET SDK](https://dotnet.microsoft.com/download/dotnet/7.0)

## Build the Docker Image

1. Clone this repository to your local machine.

2. Navigate to the project's root directory.

3. Open a terminal or command prompt.

4. Build the Docker image using the following command:

   ```bash
   docker build -t m3u8-proxy .
   ```

   This will create a Docker image named `m3u8-proxy`.

## Run the M3U8 Proxy Container

After building the Docker image, you can run the M3U8 proxy as a Docker container.

1. Use the following command to run the container:

   ```bash
   docker run -d -p 80:80 -p 443:443 -e ProxyUrl="https://domain.com/" m3u8-proxy
   ```

   This command will start the M3U8 proxy container in detached mode, mapping ports 80 and 443 on your host machine to the container. Be sure to replace `"https://domain.com/"` with your actual proxy URL, and make sure it includes the trailing `/`.

2. You can access the M3U8 proxy by opening a web browser and navigating to `http://localhost` or `https://localhost` (for HTTPS).

## Running the Project Without Docker

If you prefer to run the M3U8 proxy project without Docker, follow these steps:

1. Clone this repository to your local machine.

2. Navigate to the project's root directory.

3. Open a terminal or command prompt.

4. Set the `ProxyUrl` environment variable with your proxy URL:

   ```bash
   export ProxyUrl="https://domain.com/"
   ```

   Replace `"https://domain.com/"` with your actual proxy URL.

5. Build and run the project using the following commands:

   ```bash
   dotnet build
   dotnet run --project M3U8Proxy
   ```

   The M3U8 proxy should now be running locally, and you can access it as described earlier.

## Configuration

The M3U8 proxy is configured by default to serve content from within the container. If you need to customize the proxy configuration, you can do so by modifying the `appsettings.json` file in your project and rebuilding the Docker image or recompiling the project.

## Usage

Once the M3U8 proxy is running, you can use it to stream HLS files by making requests to the appropriate endpoints.

## Cleanup

To stop and remove the M3U8 proxy container, use the following commands:

```bash
docker stop <container_id>
docker rm <container_id>
```

Replace `<container_id>` with the actual container ID or name.

---

These instructions should help users set up and run your M3U8 proxy project both with Docker and without Docker, while also specifying the requirement to set the `ProxyUrl` environment variable.
