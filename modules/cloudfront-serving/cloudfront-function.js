function handler(event) {
  var request = event.request;
  var uri = request.uri;

  // Append index.html to paths that end with /
  if (uri.endsWith('/')) {
    request.uri = uri + 'index.html';
  }
  // Append /index.html to paths without an extension
  else if (!uri.includes('.', uri.lastIndexOf('/'))) {
    request.uri = uri + '/index.html';
  }

  return request;
}
