﻿using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using Vanara.Extensions;
using Vanara.InteropServices;
using Vanara.PInvoke;
using static Vanara.PInvoke.AdvApi32;

namespace Vanara.Security.AccessControl
{
	/// <summary>Enables access to managed <see cref="SecurityIdentifier"/> as unmanaged <see cref="PSID"/>.</summary>
	/// <seealso cref="Vanara.InteropServices.PinnedObject"/>
	public class PinnedSid : PinnedObject
	{
		private readonly byte[] bytes;

		public PinnedSid(SecurityIdentifier sid)
		{
			bytes = new byte[sid.BinaryLength];
			sid.GetBinaryForm(bytes, 0);
			SetObject(bytes);
		}

		public PSID PSID => (IntPtr)this;
	}

	/// <summary>Enables access to managed <see cref="RawAcl"/> as unmanaged <see cref="T:byte[]"/>.</summary>
	public class PinnedAcl : PinnedObject
	{
		private readonly byte[] bytes;

		public PinnedAcl(RawAcl acl)
		{
			bytes = new byte[acl.BinaryLength];
			acl.GetBinaryForm(bytes, 0);
			SetObject(bytes);
		}

		public PACL PACL => (IntPtr)this;
	}

	/// <summary>Enables access to managed <see cref="ObjectSecurity"/> as unmanaged <see cref="T:byte[]"/>.</summary>
	public class PinnedSecurityDescriptor : PinnedObject
	{
		private readonly byte[] bytes;

		public PinnedSecurityDescriptor(ObjectSecurity sd)
		{
			bytes = sd.GetSecurityDescriptorBinaryForm();
			SetObject(bytes);
		}

		public PSECURITY_DESCRIPTOR PSECURITY_DESCRIPTOR => (IntPtr)this;
	}

	/// <summary>Helper methods for working with Access Control structures.</summary>
	public static class AccessControlHelper
	{
		public static uint GetAceCount(this PACL pAcl) => pAcl.GetAclInformation<ACL_SIZE_INFORMATION>().AceCount;

		public static uint GetAclSize(PACL pAcl) => pAcl.GetAclInformation<ACL_SIZE_INFORMATION>().AclBytesInUse;

		public static uint GetEffectiveRights(this PSID pSid, PSECURITY_DESCRIPTOR pSD)
		{
			BuildTrusteeWithSid(out var t, pSid);
			GetSecurityDescriptorDacl(pSD, out var daclPresent, out var pDacl, out var daclDefaulted);
			GetEffectiveRightsFromAcl(pDacl, t, out var access);
			return access;
		}

		public static uint GetEffectiveRights(this RawSecurityDescriptor sd, SecurityIdentifier sid)
		{
			BuildTrusteeWithSid(out var t, GetPSID(sid));
			using (var pDacl = new PinnedAcl(sd.DiscretionaryAcl))
			{
				GetEffectiveRightsFromAcl(pDacl.PACL, t, out var access);
				return access;
			}
		}

		public static PSID GetPSID(this SecurityIdentifier sid) { using (var ps = new PinnedSid(sid)) return ps.PSID; }

		public static RawAcl RawAclFromPtr(PACL pAcl)
		{
			var len = GetAclSize(pAcl);
			var dest = new byte[len];
			Marshal.Copy((IntPtr)pAcl, dest, 0, (int)len);
			return new RawAcl(dest, 0);
		}

		public static string ToSddl(this PSECURITY_DESCRIPTOR pSD, SECURITY_INFORMATION si) => ConvertSecurityDescriptorToStringSecurityDescriptor(pSD, si);

		public static string ToSddl(this SafeSecurityDescriptor pSD, SECURITY_INFORMATION si) => ConvertSecurityDescriptorToStringSecurityDescriptor(pSD, si);
	}
}