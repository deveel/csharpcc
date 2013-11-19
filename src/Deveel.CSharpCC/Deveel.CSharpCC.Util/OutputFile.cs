using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

using Deveel.CSharpCC.Parser;

namespace Deveel.CSharpCC.Util {
	internal class OutputFile {
		private TrapCloseTextWriter pw;
		private DigestOutputStream dos;
		private String toolName = CSharpCCGlobals.ToolName;

		private readonly string file;
		private readonly String compatibleVersion;
		private readonly String[] options;

		internal bool needToWrite;

		private const String MD5_LINE_PART_1 = "/* CSharpCC - OriginalChecksum=";
		private const String MD5_LINE_PART_1q = "/\\* CSharpCC - OriginalChecksum=";
		private const String MD5_LINE_PART_2 = " (do not edit this line) */";
		private const String MD5_LINE_PART_2q = " \\(do not edit this line\\) \\*/";


		public OutputFile(string file, String compatibleVersion, String[] options) {
			this.file = file;
			this.compatibleVersion = compatibleVersion;
			this.options = options;

			if (File.Exists(file)) {
				// Generate the checksum of the file, and compare with any value
				// stored
				// in the file.

				StreamReader br = new StreamReader(file);
				MD5 digest;
				try {
					digest = MD5.Create("MD5");
				} catch (TargetInvocationException e) {
					throw new IOException("No MD5 implementation", e);
				}

				DigestOutputStream digestStream = new DigestOutputStream(Stream.Null, digest);
				StreamWriter pw = new StreamWriter(digestStream);
				String line;
				String existingMD5 = null;
				while ((line = br.ReadLine()) != null) {
					if (line.StartsWith(MD5_LINE_PART_1)) {
						existingMD5 = line.Replace(MD5_LINE_PART_1q, "")
							.Replace(MD5_LINE_PART_2q, "");
					} else {
						pw.WriteLine(line);
					}
				}

				pw.Close();
				String calculatedDigest = ToHexString(digestStream.Hash);

				if (existingMD5 == null || !existingMD5.Equals(calculatedDigest)) {
					// No checksum in file, or checksum differs.
					needToWrite = false;

					if (compatibleVersion != null) {
						CheckVersion(file, compatibleVersion);
					}

					if (options != null) {
						CheckOptions(file, options);
					}

				} else {
					// The file has not been altered since JavaCC created it.
					// Rebuild it.
					Console.Out.WriteLine("File \"" + Path.GetFileName(file) + "\" is being rebuilt.");
					needToWrite = true;
				}
			} else {
				// File does not exist
				Console.Out.WriteLine("File \"" + Path.GetFileName(file) + "\" does not exist.  Will create one.");
				needToWrite = true;
			}
		}

		public OutputFile(string file)
			: this(file, null, null) {
		}

		public string ToolName {
			get { return toolName; }
			set { toolName = value; }
		}

		private void CheckVersion(string file, String versionId) {
			String firstLine = "/* " + CSharpCCGlobals.GetIdString(ToolName, Path.GetFileName(file)) + " Version ";

			try {
				StreamReader reader = new StreamReader(file);

				String line;
				while ((line = reader.ReadLine()) != null) {
					if (line.StartsWith(firstLine)) {
						String version = firstLine.Replace(".* Version ", "").Replace(" \\*/", "");
						if (version != versionId) {
							CSharpCCErrors.Warning(Path.GetFileName(file)
								+ ": File is obsolete.  Please rename or delete this file so"
								+ " that a new one can be generated for you.");
						}
						return;
					}
				}
				// If no version line is found, do not output the warning.
			} catch (FileNotFoundException e1) {
				// This should never happen
				CSharpCCErrors.SemanticError("Could not open file " + Path.GetFileName(file) + " for writing.");
				throw new InvalidOperationException();
			} catch (IOException e2) {
			}
		}

		private void CheckOptions(string file, string[] options) {
			try {
				StreamReader reader = new StreamReader(file);

				String line;
				while ((line = reader.ReadLine()) != null) {
					if (line.StartsWith("/* CSharpCCOptions:")) {
						String currentOptions = Options.GetOptionsString(options);
						if (line.IndexOf(currentOptions) == -1) {
							CSharpCCErrors.Warning(Path.GetFileName(file)
								+ ": Generated using incompatible options. Please rename or delete this file so"
								+ " that a new one can be generated for you.");
						}
						return;
					}
				}
			} catch (FileNotFoundException e1) {
				// This should never happen
				CSharpCCErrors.SemanticError("Could not open file " + Path.GetFileName(file)
					+ " for writing.");
				throw new InvalidOperationException();
			} catch (IOException e2) {
			}

			// Not found so cannot check
		}

		public void Close() {
			// Write the trailer (checksum).
			// Possibly rename the .java.tmp to .java??
			if (pw != null) {
				pw.WriteLine(MD5_LINE_PART_1 + GetMd5Sum() + MD5_LINE_PART_2);
				pw.CloseWriter();
			}
		}

		private String GetMd5Sum() {
			pw.Flush();
			byte[] digest = dos.Hash;
			return ToHexString(digest);
		}

		public TextWriter GetTextWriter() {
			if (pw == null) {
				HashAlgorithm digest;
				try {
					digest = HashAlgorithm.Create("MD5");
				} catch (TargetException e) {
					throw new IOException("No MD5 implementation", e);
				}

				dos = new DigestOutputStream(new FileStream(file, FileMode.OpenOrCreate, FileAccess.Write), digest);
				pw = new TrapCloseTextWriter(this, dos);

				// Write the headers....
				String version = compatibleVersion ?? typeof(OutputFile).Assembly.GetName().Version.ToString();
				pw.WriteLine("/* " + CSharpCCGlobals.GetIdString(toolName, Path.GetFileName(file)) + " Version " + version + " */");
				if (options != null) {
					pw.WriteLine("/* CSharpCCOptions:" + Options.GetOptionsString(options) + " */");
				}
			}

			return pw;
		}


		private readonly static char[] HEX_DIGITS = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 'd', 'e', 'f' };

		private static string ToHexString(byte[] bytes) {
			StringBuilder sb = new StringBuilder(32);
			for (int i = 0; i < bytes.Length; i++) {
				byte b = bytes[i];
				sb.Append(HEX_DIGITS[(b & 0xF0) >> 4]).Append(HEX_DIGITS[b & 0x0F]);
			}
			return sb.ToString();
		}



		private class TrapCloseTextWriter : StreamWriter {
			private readonly OutputFile file;

			public TrapCloseTextWriter(OutputFile file, Stream stream)
				: base(stream) {
				this.file = file;
			}

			public void CloseWriter() {
				base.Close();
			}

			public override void Close() {
				try {
					file.Close();
				} catch (Exception e) {
					Console.Out.WriteLine("Could not close writer: " + e.Message);
				}
			}
		}

		#region DigestOutputStream

		class DigestOutputStream : Stream {
			private readonly Stream output;
			private readonly HashAlgorithm hasher;

			public DigestOutputStream(Stream output, HashAlgorithm hasher) {
				this.output = output;
				this.hasher = hasher;
			}

			public byte[] Hash {
				get { return hasher.Hash; }
			}

			public override void Flush() {
				hasher.TransformFinalBlock(new byte[0], 0, 0);
				output.Flush();
			}

			public override long Seek(long offset, SeekOrigin origin) {
				return output.Seek(offset, origin);
			}

			public override void SetLength(long value) {
				output.SetLength(value);
			}

			public override int Read(byte[] buffer, int offset, int count) {
				throw new NotSupportedException();
			}

			public override void Write(byte[] buffer, int offset, int count) {
				hasher.TransformBlock(buffer, offset, count, null, 0);
				output.Write(buffer, offset, count);
			}

			public override bool CanRead {
				get { return false; }
			}

			public override bool CanSeek {
				get { return true; }
			}

			public override bool CanWrite {
				get { return true; }
			}

			public override long Length {
				get { return output.Length; }
			}

			public override long Position {
				get { return output.Position; }
				set { output.Position = value; }
			}
		}

		#endregion
	}
}