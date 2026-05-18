function fileSelector() {
    const maxPreviewFileSize = 10 * 1024 * 1024;

    function isSafeImageSource(value) {
        return typeof value === "string" && /^(https?:\/\/|\/|data:image\/)/i.test(value);
    }

    function isValidDataImageUrl(value) {
        return typeof value === "string" && /^data:image\/[a-zA-Z0-9.+-]+;base64,/i.test(value);
    }

    return {
        imageForUser: "",
        init() {
            const initialImage = this.$el.dataset.initialImage || "";
            this.imageForUser = isSafeImageSource(initialImage) ? initialImage : "";
        },
        fileInput: {
            ["@change"]($event) {
                const file = $event.target.files[0];
                if (!file || !file.type.startsWith("image/") || file.size > maxPreviewFileSize) {
                    this.imageForUser = "";
                    return;
                }

                const reader = new FileReader();
                reader.onload = (e) => {
                    if (!isValidDataImageUrl(e.target?.result)) {
                        console.warn("Invalid image data URL for preview.");
                        this.imageForUser = "";
                        return;
                    }

                    this.imageForUser = e.target.result;
                };
                reader.readAsDataURL(file);
            }
        },
        selector: {
            ["@click"]() {
                this.$refs.fileInput.click();
            }
        }
    };
}
