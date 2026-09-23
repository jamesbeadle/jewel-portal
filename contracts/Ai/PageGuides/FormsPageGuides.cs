namespace Jewel.JPMS.Contracts.Ai;

/// <summary>The forms new starters and sub-contractors fill in, the office's screens over them, and the two public pages. Data only.</summary>
public static class FormsPageGuides
{
    public static readonly IReadOnlyList<PageGuide> Guides = new PageGuide[]
    {
        new("/forms", "Forms",
            "Received: every form that came in, for both Jewel companies, newest first — chips narrow by company and by where "
            + "the office has got to (To handle = New and In progress). A row opens the form. ?folder= lists one person's or "
            + "company's forms. Tabs lead to New starter packs, Sent out, Right to work (its readers only), Training, "
            + "Workstations and People & companies. Assistant: list_form_submissions, get_form_submission."),
        new("/forms/received/{form}", "Form",
            "One form read in the form's own order, files under the question that asked for them, a PDF of the record, and "
            + "the status menu (New, In progress, Handled). Above the answers sits the thing the office does with this kind "
            + "of form: accept a training certificate onto the register, file a questionnaire's or insurance update's "
            + "certificates to a directory company, record a company vehicle form's licence check (which deletes the photo "
            + "and withholds the driving record), record the right-to-work check, or close a workstation assessment's "
            + "actions. Emergency health answers are hidden until Show health answers, each look audited. Assistant: "
            + "get_form_submission, set_form_submission_status, accept_training_certificate, file_form_to_directory, "
            + "record_driving_licence_check, save_right_to_work_check, resolve_workstation_action."),
        new("/forms/packs", "New starter packs",
            "One link per new starter to every form they owe, and the one screen of done and outstanding. Send a pack asks "
            + "who, for which company, how they are engaged and four questions (P45, screen work, a company vehicle, a "
            + "ticket), and shows the forms before it sends. Each row's menu chases (a new link, fourteen fresh days; what "
            + "is done stays done) or cancels. Assistant: list_form_packs, send_form_pack, chase_form_pack, cancel_form_pack."),
        new("/forms/sent", "Forms sent out",
            "Forms sent on their own to one named person, with whether each link has been opened or used. Send a form "
            + "picks the form, the company, the person and how long the link lasts; a row's menu sends it again (the old "
            + "link dies) or cancels it. Assistant: list_form_invites, send_form_invite, resend_form_invite, cancel_form_invite."),
        new("/forms/right-to-work", "Right to work",
            "The checker's register, behind its own readers: each check cleared, not finished (with what is missing) or do "
            + "not start, its expiry and follow-up, late checks, evidence, and Send confirmation for a cleared one. Record "
            + "a check is the dashboard's six numbered steps. Assistant: list_right_to_work_checks, save_right_to_work_check, "
            + "send_right_to_work_confirmation."),
        new("/forms/training", "Training",
            "Tickets and certificates by expiry — the holder is asked for the renewal a month before. A row opens its "
            + "email, expiry and leaving date. Assistant: list_training_records, set_training_record_details."),
        new("/forms/workstations", "Workstations",
            "Every NO from every workstation assessment, open first; a row closes it as fixed or accepted with a note. "
            + "Assistant: list_workstation_actions, resolve_workstation_action."),
        new("/forms/folders", "People & companies",
            "The people and companies forms are filed under, with the two dates that start their destruction clocks — the "
            + "day an engagement ended and the day a company vehicle came back. Assistant: list_form_folders, "
            + "record_form_folder_dates."),
        new("/emergency-contacts", "Emergency contacts",
            "Each person's latest emergency contact as a card, for whoever is on site. Health answers show only after Show "
            + "health answers, each look audited, and never over the connector. Assistant: list_emergency_contacts."),
        new("/f/{company}/{form}", "Public form",
            "The page a new starter or sub-contractor fills in on a phone, no sign-in — the company's own paper (Jewel "
            + "Property Serve in its blue, Jewel Bespoke Build in black). Opened by its open address, a one-time link (?k=) "
            + "or from a pack (?p=). Staff never fill it in; what it sends lands on /forms."),
        new("/f/{company}/pack/{token}", "New starter pack (public)",
            "A new starter's one link: the forms they owe, each ticked as it is sent. Staff never use this page.")
    };
}
