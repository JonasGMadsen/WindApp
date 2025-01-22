export async function map_init(lat = 55.6761, lon = 12.5683, zoom = 10) {
    const isTouchDevice = (
        ('ontouchstart' in window) ||
        (navigator.maxTouchPoints > 0) ||
        (navigator.msMaxTouchPoints > 0)
    );

    var map = L.map("map").setView([lat, lon], zoom);

    L.tileLayer("https://tile.openstreetmap.org/{z}/{x}/{y}.png", {
        maxZoom: 19,
        attribution: '&copy; <a href="http://www.openstreetmap.org/copyright">OpenStreetMap</a>'
    }).addTo(map);

    // try to fetch local wind data
    let jsonData = null;
    try {
        let localResponse = await fetch('/api/WindData/local');
        if (localResponse.ok) {
            jsonData = await localResponse.json();
        } else if (localResponse.status === 404) {
            console.log("Local winddata.json not found. Will fetch remote compressed data.");
        } else {
            throw new Error(`Fetching local data failed with status: ${localResponse.status}`);
        }
    } catch (e) {
        console.error("Error fetching local data:", e);
    }

    // fetch the remote Brotli-compressed file & call the decompress, if no local data is found
    if (!jsonData) {
        const remoteResponse = await fetch('/api/WindData/remote');
        if (!remoteResponse.ok) {
            throw new Error(`Failed to fetch remote compressed data. Status: ${remoteResponse.status}`);
        }

        jsonData = await decompressBrotliResponse(remoteResponse);
    }

    let marker;

    map.on("click", function (e) {
        if (isTouchDevice) {
            if (marker) {
                map.removeLayer(marker);
            }
            marker = L.marker(e.latlng).addTo(map);
        }
    });
    //The velocityLayer will always refresh when the map is moved or zoomed. This is an inherent feature of the Leaflet-Velocity
    var velocityLayer = L.velocityLayer({
        displayValues: true,
        displayOptions: {
            velocityType: 'Global Wind',
            position: 'bottomleft',
            emptyString: 'No wind data'
        },
        data: jsonData, // pass the data array directly
        maxVelocity: 20,   // control max veclocity
        velocityScale: 0.04, // tweak to control vector size
    });

    velocityLayer.addTo(map);
}

async function decompressBrotliResponse(response) {
    const brotliModule = await import("https://unpkg.com/brotli-wasm@3.0.0/index.web.js?module");
    //https://github.com/httptoolkit/brotli-wasm/tree/main?tab=readme-ov-file#in-browser-with-streams

    const {
        DecompressStream,
        BrotliStreamResultCode
    } = await brotliModule.default;



    const decompressStream = new DecompressStream();

    return new Promise(async (resolve, reject) => {
        const OUTPUT_SIZE = 1024;

        const decompressionStream = new TransformStream({
            transform(chunk, controller) {
                let resultCode;
                let inputOffset = 0;
                do {
                    const input = chunk.slice(inputOffset);
                    const result = decompressStream.decompress(input, OUTPUT_SIZE);

                    controller.enqueue(result.buf);

                    resultCode = result.code;
                    inputOffset += result.input_offset;

                } while (resultCode === BrotliStreamResultCode.NeedsMoreOutput);


                if (
                    resultCode !== BrotliStreamResultCode.NeedsMoreInput &&
                    resultCode !== BrotliStreamResultCode.ResultSuccess
                ) {
                    controller.error(`Brotli decompression failed with code: ${resultCode}`);
                }
            },
            flush(controller) {
                controller.terminate();
            }
        });

        const textDecoderStream = new TextDecoderStream();

        let jsonBuffer = '';

        const outputStream = new WritableStream({
            write(chunk) {
                jsonBuffer += chunk;
            },
            close() {
                try {
                    const jsonObject = JSON.parse(jsonBuffer);
                    resolve(jsonObject);
                } catch (error) {
                    reject(`Failed to parse JSON: ${error}`);
                }
            }
        });

        try {
            //readable stream
            await response.body
                //transform stream
                .pipeThrough(decompressionStream)
                //transform stream
                .pipeThrough(textDecoderStream)
                //writable stream
                .pipeTo(outputStream);
        } catch (err) {
            reject(err);
        }
    });
}

