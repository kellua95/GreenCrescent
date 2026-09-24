export async function printApplication() {
    const source = document.querySelector(".printable-application");

    if (!source) {
        throw new Error("لم يتم العثور على تفاصيل الطلب.");
    }

    document.getElementById("orphan-print-frame")?.remove();

    const copy = source.cloneNode(true);

    copy.querySelectorAll(
        ".no-print, .decision-panel, button"
    ).forEach(element => element.remove());

    // تحويل رابط الشعار إلى رابط كامل.
    copy.querySelectorAll("img").forEach(img => {
        img.src = new URL(img.getAttribute("src"), document.baseURI).href;
    });

    const frame = document.createElement("iframe");
    frame.id = "orphan-print-frame";
    frame.title = "طباعة طلب كفالة يتيم";
    frame.setAttribute("aria-hidden", "true");

    Object.assign(frame.style, {
        position: "fixed",
        left: "-10000px",
        top: "0",
        width: "210mm",
        height: "297mm",
        border: "0"
    });

    const loaded = new Promise((resolve, reject) => {
        frame.onload = resolve;
        frame.onerror = () => reject(
            new Error("تعذر تجهيز صفحة الطباعة.")
        );
    });

    frame.srcdoc = `
<!DOCTYPE html>
<html lang="ar" dir="rtl">
<head>
    <meta charset="utf-8">
    <title>طلب كفالة يتيم</title>
    <style>
        @page {
            size: A4 portrait;
            margin: 12mm;
        }

        * {
            box-sizing: border-box;
        }

        body {
            margin: 0;
            color: #111;
            background: white;
            font-family: Tahoma, Arial, sans-serif;
            font-size: 10pt;
            line-height: 1.5;
        }

        h2, h3, p {
            margin-top: 0;
        }

        .print-heading {
            display: flex;
            align-items: center;
            justify-content: space-between;
            gap: 12px;
            padding-bottom: 12px;
            margin-bottom: 16px;
            border-bottom: 2px solid #08783e;
            break-inside: avoid;
        }

        .print-heading img {
            width: 65px;
            height: 65px;
            object-fit: contain;
        }

        .print-heading h2 {
            font-size: 14pt;
            margin-bottom: 4px;
        }

        .print-heading h3 {
            font-size: 11pt;
            border: 0;
            margin: 4px 0;
            padding: 0;
        }

        .print-heading p {
            margin: 3px 0;
        }

        .print-reference {
            max-width: 38%;
            font-size: 8pt;
            overflow-wrap: anywhere;
        }

        .details-heading {
            margin-bottom: 12px;
            break-inside: avoid;
        }

        .details-heading h2 {
            font-size: 13pt;
            margin-bottom: 4px;
        }

        h3 {
            margin: 14px 0 7px;
            padding-bottom: 5px;
            border-bottom: 1px solid #bbb;
            font-size: 11pt;
            break-after: avoid;
        }

        .details-grid {
            display: grid;
            grid-template-columns: repeat(3, minmax(0, 1fr));
            gap: 6px;
            margin: 0;
        }

        .details-grid > div {
            padding: 6px;
            border: 1px solid #ddd;
            min-width: 0;
            break-inside: avoid;
        }

        dt {
            color: #555;
            font-size: 8pt;
            margin-bottom: 3px;
        }

        dd {
            margin: 0;
            white-space: pre-wrap;
            overflow-wrap: anywhere;
        }

        table {
            width: 100%;
            border-collapse: collapse;
            table-layout: fixed;
            font-size: 8pt;
            margin-top: 10px;
        }

        th, td {
            padding: 5px;
            border: 1px solid #bbb;
            text-align: right;
            white-space: normal;
            overflow-wrap: anywhere;
        }

        thead {
            display: table-header-group;
        }

        tr {
            break-inside: avoid;
        }

        .notice {
            border: 1px solid #aaa;
            padding: 8px;
            margin-bottom: 12px;
        }

        .print-signatures {
            display: flex;
            justify-content: space-between;
            gap: 15px;
            margin-top: 25px;
            padding-top: 12px;
            border-top: 1px solid #aaa;
            break-inside: avoid;
        }

        .print-signatures > div {
            flex: 1;
            text-align: center;
        }

        .print-signatures p {
            margin-top: 28px;
        }
        .orphan-photo {
            margin-bottom: 12px;
            break-inside: avoid;
        }

        .orphan-photo img {
            width: 30mm;
            height: 38mm;
            object-fit: contain;
            border: 1px solid #aaa;
        }
    </style>
</head>
<body>${copy.outerHTML}</body>
</html>`;

    document.body.appendChild(frame);

    try {
        await loaded;

        const printDocument = frame.contentDocument;
        const printWindow = frame.contentWindow;

        await Promise.all(
            Array.from(printDocument.images).map(img =>
                img.decode().catch(() => { })
            )
        );

        await printDocument.fonts.ready;

        // إتاحة وقت للمتصفح لحساب توزيع المحتوى.
        await new Promise(resolve =>
            printWindow.requestAnimationFrame(() =>
                printWindow.requestAnimationFrame(resolve)
            )
        );

        printWindow.focus();
        printWindow.print();
    } catch (error) {
        frame.remove();
        throw error;
    }

    // يبقى الإطار خارج الشاشة ويُستبدل عند الطباعة التالية.
}