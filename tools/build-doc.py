# -*- coding: utf-8 -*-
"""把仓库里的 Markdown 文档渲染成单文件 HTML，方便直接发给别人。

用法:
    python tools/build-doc.py docs/协作者安装指南.md -o docs/协作者安装指南.html
"""
import argparse
import html
import re
import sys
from pathlib import Path

try:
    import markdown
except ImportError:
    sys.exit("需要 markdown 库: python -m pip install markdown")

CSS = """
:root{
  --ink:#1a1a1a; --muted:#5c6370; --line:#e3e6ea; --bg:#ffffff;
  --accent:#1f6feb; --code-bg:#f6f8fa; --warn-bg:#fff8e6; --warn-line:#e0a800;
}
*{box-sizing:border-box}
body{
  margin:0; background:#f7f8fa; color:var(--ink);
  font:16px/1.75 -apple-system,"Segoe UI","PingFang SC","Microsoft YaHei",sans-serif;
  -webkit-font-smoothing:antialiased;
}
.page{max-width:880px; margin:0 auto; background:var(--bg); padding:56px 64px 96px;
  box-shadow:0 1px 3px rgba(0,0,0,.06);}
h1{font-size:30px; line-height:1.35; margin:0 0 8px; letter-spacing:-.4px;}
h2{font-size:21px; margin:44px 0 14px; padding-top:18px; border-top:1px solid var(--line);}
h3{font-size:17px; margin:28px 0 10px;}
p{margin:12px 0;}
a{color:var(--accent); text-decoration:none;}
a:hover{text-decoration:underline;}
hr{border:0; border-top:1px solid var(--line); margin:36px 0;}
code{background:var(--code-bg); padding:2px 5px; border-radius:4px;
  font:13.5px/1.5 "SFMono-Regular",Consolas,"Liberation Mono",monospace;}
pre{background:var(--code-bg); padding:14px 16px; border-radius:8px; overflow-x:auto;
  border:1px solid var(--line);}
pre code{background:none; padding:0; font-size:13px;}
table{border-collapse:collapse; width:100%; margin:16px 0; font-size:14.5px;}
th,td{border:1px solid var(--line); padding:8px 12px; text-align:left; vertical-align:top;}
th{background:#f2f4f7; font-weight:600; white-space:nowrap;}
tr:nth-child(even) td{background:#fafbfc;}
blockquote{margin:16px 0; padding:12px 16px; background:var(--warn-bg);
  border-left:3px solid var(--warn-line); color:#4a3c00; border-radius:0 6px 6px 0;}
blockquote p{margin:4px 0;}
ul,ol{padding-left:24px; margin:12px 0;}
li{margin:5px 0;}
li input[type=checkbox]{margin-right:6px; transform:translateY(1px);}
.toc{background:#f7f8fa; border:1px solid var(--line); border-radius:8px;
  padding:14px 20px; margin:24px 0 8px; font-size:14.5px;}
.toc b{display:block; font-size:13px; letter-spacing:.06em; color:var(--muted);
  text-transform:uppercase; margin-bottom:8px;}
.toc a{display:block; padding:2px 0; color:#333;}
.meta{color:var(--muted); font-size:13.5px; margin-bottom:26px;}
@media print{
  body{background:#fff;} .page{box-shadow:none; padding:0; max-width:none;}
  h2{page-break-after:avoid;} pre,table,blockquote{page-break-inside:avoid;}
}
@media (max-width:720px){
  .page{padding:28px 20px 64px;} h1{font-size:24px;}
}
"""


def build_toc(body: str) -> str:
    items = re.findall(r'<h2 id="([^"]+)">(.*?)</h2>', body, re.S)
    if len(items) < 3:
        return ""
    links = "\n".join(
        f'<a href="#{i}">{re.sub(r"<[^>]+>", "", t)}</a>' for i, t in items
    )
    return f'<nav class="toc"><b>目录</b>{links}</nav>'


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("src")
    ap.add_argument("-o", "--out")
    ap.add_argument("--title")
    args = ap.parse_args()

    src = Path(args.src)
    text = src.read_text(encoding="utf-8")

    # 去掉首行 H1，单独渲染成页面标题，避免重复
    lines = text.splitlines()
    title = args.title
    if lines and lines[0].startswith("# "):
        title = title or lines[0][2:].strip()
        text = "\n".join(lines[1:])
    title = title or src.stem

    body = markdown.markdown(
        text,
        extensions=["tables", "fenced_code", "sane_lists", "toc", "attr_list"],
        extension_configs={"toc": {"permalink": False}},
    )

    # Markdown 的 "- [ ]" 原生不转复选框，这里补上，清单才有可读性
    body = re.sub(r"<li>\[ \]\s*", '<li><input type="checkbox" disabled> ', body)
    body = re.sub(r"<li>\[[xX]\]\s*", '<li><input type="checkbox" checked disabled> ', body)

    page = f"""<!DOCTYPE html>
<html lang="zh-CN">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width,initial-scale=1">
<title>{html.escape(title)}</title>
<style>{CSS}</style>
</head>
<body>
<div class="page">
<h1>{html.escape(title)}</h1>
<div class="meta">Crown Siege Mod · 内部文档 · 有疑问直接找 Lee</div>
{build_toc(body)}
{body}
</div>
</body>
</html>
"""

    out = Path(args.out) if args.out else src.with_suffix(".html")
    out.write_text(page, encoding="utf-8")
    print(f"written: {out}  ({out.stat().st_size} bytes)")


if __name__ == "__main__":
    main()
