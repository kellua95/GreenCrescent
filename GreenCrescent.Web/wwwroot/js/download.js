window.downloadBase64File = (fileName, contentType, base64Data) => {
    const byteCharacters = atob(base64Data);
    const byteNumbers = new Array(byteCharacters.length);

    for (let index = 0; index < byteCharacters.length; index++) {
        byteNumbers[index] = byteCharacters.charCodeAt(index);
    }

    const blob = new Blob(
        [new Uint8Array(byteNumbers)],
        { type: contentType });

    const url = URL.createObjectURL(blob);
    const anchor = document.createElement("a");

    anchor.href = url;
    anchor.download = fileName;
    anchor.click();

    URL.revokeObjectURL(url);
};