import os, chardet # pip install chardet

def Execution(path):
  if not path.rsplit(".", 1)[-1] in ("xml", "axaml", "cs"): return
  with open(path, "rb") as file: data = file.read()
  enc = chardet.detect(data)["encoding"]
  if enc != "ascii" and enc != "utf-8":
    # ;'-} Не обязательный костыль, но из-за него будут появляться невидимые
    # изменения в первой строке во время обозревания содержимого комита:
    if enc == "UTF-8-SIG": return
    data2 = data.decode(enc).encode("utf-8") # Основное место вправления человечности
    print(path, enc, len(data), len(data2))
    with open(path, "wb") as file: file.write(data2)

def Recurs(path):
  for name in os.listdir(path):
    if name in ("obj", "bin", ".git", ".vs"): continue
    path2 = path + name
    if os.path.isdir(path2): Recurs(path2 + "\\")
    elif os.path.isfile(path2): Execution(path2)

path = __file__.rsplit("\\", 4)[0] + "\\"
Recurs(path)
#print(Recurs.__code__.co_consts)
