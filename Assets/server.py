from flask import Flask, request, Response
import subprocess

app = Flask(__name__)

@app.route('/run', methods=['POST'])
def run_code():
    code = request.form.get('code')
    if not code:
        return Response("No code provided", status=400)

    try:
        # Run JavaScript code using Node.js
        result = subprocess.run(
            ["node", "-e", code], capture_output=True, text=True, timeout=5
        )
        output = result.stdout + result.stderr
        return Response(output, mimetype="text/plain")
    except subprocess.TimeoutExpired:
        return Response("Code execution timed out", status=400)
    except Exception as e:
        return Response(str(e), status=400)
    
if __name__ == '__main__':
    app.run(debug=True, host='0.0.0.0', port=5000)
