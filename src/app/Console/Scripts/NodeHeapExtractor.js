const http = require('http');
const fs = require('fs');

// Read the target path from the environment variable set by the C# CLI
const dumpPath = process.env.DUMP_PATH || '/tmp/node_dump.heapsnapshot';

http.get('http://127.0.0.1:9229/json/list', (res) => {
    let data = '';
    res.on('data', c => data += c);
    res.on('end', () => {
        const target = JSON.parse(data)[0];
        console.log('Connecting to: ' + target.webSocketDebuggerUrl);
        
        const ws = new WebSocket(target.webSocketDebuggerUrl);
        const fileStream = fs.createWriteStream(dumpPath);

        ws.onopen = () => {
            console.log('Connected! Requesting heap snapshot...');
            ws.send(JSON.stringify({ id: 1, method: 'HeapProfiler.takeHeapSnapshot' }));
        };
        
        ws.onmessage = (msg) => {
            const response = JSON.parse(msg.data);
            if (response.method === 'HeapProfiler.addHeapSnapshotChunk') {
                fileStream.write(response.params.chunk);
            }
            if (response.id === 1) {
                console.log('Snapshot complete! Flushing to disk...');
                fileStream.end(() => {
                    process.exit(0);
                });
            }
        };
        
        ws.onerror = (err) => console.error('WebSocket Error:', err);
    });
}).on('error', (err) => {
    console.error('Failed to connect to Inspector:', err.message);
    process.exit(1);
});