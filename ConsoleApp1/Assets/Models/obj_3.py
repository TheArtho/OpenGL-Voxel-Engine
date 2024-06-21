def convert_quads_to_triangles(source_path, target_path):
    with open(source_path, 'r') as file:
        lines = file.readlines()

    with open(target_path, 'w') as file:
        for line in lines:
            if line.startswith('f ') and len(line.split()) == 5:
                # Extractions des vertex pour un quad
                _, v1, v2, v3, v4 = line.split()
                # Création de deux triangles à partir du quad
                triangle1 = f"f {v1} {v2} {v3}\n"
                triangle2 = f"f {v3} {v4} {v1}\n"
                file.write(triangle1)
                file.write(triangle2)
            else:
                # Écriture de la ligne inchangée si ce n'est pas un quad
                file.write(line)

def main():
    import sys
    if len(sys.argv) != 3:
        print("Usage: python convert_obj.py <source.obj> <target.obj>")
        return

    source_file = sys.argv[1]
    target_file = sys.argv[2]
    convert_quads_to_triangles(source_file, target_file)
    print("Conversion completed.")

if __name__ == "__main__":
    main()
